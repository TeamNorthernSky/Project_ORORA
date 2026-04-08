using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public class CSVDataLoad : MonoBehaviour
{
    [Tooltip("레거시: playerUnitCsv가 비어 있을 때 플레이어 유닛 CSV로 사용")]
    [SerializeField] private TextAsset unitCsv;

    [SerializeField] private TextAsset playerUnitCsv;
    [SerializeField] private TextAsset enemyUnitCsv;

    /// <summary>레거시 호환: 플레이어 유닛 목록과 동일.</summary>
    public List<UnitData> LoadUnits()
    {
        return LoadPlayerUnits();
    }

    public List<UnitData> LoadPlayerUnits()
    {
        TextAsset ta = playerUnitCsv != null ? playerUnitCsv : unitCsv;
        if (ta == null)
        {
            Debug.LogError("[CSVDataLoad] playerUnitCsv 또는 unitCsv가 할당되지 않았습니다.");
            return new List<UnitData>();
        }

        var all = CSVLoader.LoadUnitData(ta.text);

        if (enemyUnitCsv != null)
        {
            Debug.Log($"[CSVDataLoad] 플레이어 전용 CSV에서 유닛 {all.Count}행 로드.");
            return all;
        }

        var players = new List<UnitData>();
        for (int i = 0; i < all.Count; i++)
        {
            var u = all[i];
            if (u == null) continue;
            if (!CSVLoader.IsEnemyUnitRow(u))
            {
                players.Add(u);
            }
        }

        Debug.Log($"[CSVDataLoad] 단일 CSV에서 플레이어 유닛 {players.Count}행 (적 행 제외).");
        return players;
    }

    public List<EnemyData> LoadEnemyUnits()
    {
        if (enemyUnitCsv != null)
        {
            var list = CSVLoader.LoadEnemyData(enemyUnitCsv.text);
            Debug.Log($"[CSVDataLoad] 적 전용 CSV에서 적 {list.Count}행 로드.");
            return list;
        }

        TextAsset ta = playerUnitCsv != null ? playerUnitCsv : unitCsv;
        if (ta == null)
        {
            Debug.LogWarning("[CSVDataLoad] 적 CSV가 없고 플레이어 단일 CSV도 없어 적 데이터가 비어 있습니다.");
            return new List<EnemyData>();
        }

        var all = CSVLoader.LoadUnitData(ta.text);
        var enemies = new List<EnemyData>();
        for (int i = 0; i < all.Count; i++)
        {
            var u = all[i];
            if (u == null) continue;
            if (CSVLoader.IsEnemyUnitRow(u))
            {
                enemies.Add(CSVLoader.UnitDataToEnemyData(u, string.Empty));
            }
        }

        Debug.Log($"[CSVDataLoad] 단일 CSV에서 적 유닛 {enemies.Count}행 (IsEnemy / ID 20000~29999).");
        return enemies;
    }
}

public static class CSVLoader
{
    // CSV: 헤더 1줄 + 설명 16줄(총 17줄) 스킵, 18번째 줄부터 데이터 파싱
    private const int DataStartLinePlayerIndex0Based = 17;
    private const int DataStartLineEnemyIndex0Based = 12;

    /// <summary>Index가 20000~29999이면 적으로 간주 (플레이어 10000~19999와 분리).</summary>
    public static bool IsEnemyUnitIndex(string index)
    {
        if (string.IsNullOrWhiteSpace(index)) return false;
        if (!int.TryParse(index.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
        {
            return false;
        }

        return id >= 20000 && id < 30000;
    }

    /// <summary>CSV IsEnemy 열 또는 Index 규칙으로 적 행 여부.</summary>
    public static bool IsEnemyUnitRow(UnitData u)
    {
        if (u == null) return false;
        if (u.IsEnemyRow) return true;
        return IsEnemyUnitIndex(u.Index);
    }

    public static EnemyData UnitDataToEnemyData(UnitData u, string aiType)
    {
        if (u == null) return null;
        return new EnemyData
        {
            Index = u.Index,
            UnitType = u.UnitType,
            Name = u.Name,
            baseStats = u.baseStats,
            IsEnemyRow = u.IsEnemyRow,
            //aiType = aiType ?? string.Empty
        };
    }

    public static List<UnitData> LoadUnitData(string csvText)
    {
        var result = new List<UnitData>();
        if (string.IsNullOrWhiteSpace(csvText))
        {
            return result;
        }

        csvText = csvText.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = csvText.Split('\n');
        if (lines.Length <= DataStartLinePlayerIndex0Based)
        {
            return result;
        }

        var headerFields = ParseCsvLine(lines[0]);
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerFields.Count; i++)
        {
            var key = NormalizeHeader(headerFields[i]);
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            if (!headerIndex.ContainsKey(key))
            {
                headerIndex.Add(key, i);
            }
        }

        int TryGetCol(string headerName)
        {
            return headerIndex.TryGetValue(headerName, out var col) ? col : -1;
        }

        int hpCol = TryGetCol("HP");
        int atkCol = TryGetCol("Atk");
        int defCol = TryGetCol("DEF");
        if (defCol < 0) defCol = TryGetCol("Def");

        int luckCol = TryGetCol("Luck");

        int speedCol = TryGetCol("Speed");
        int criticalRateCol = TryGetCol("CriticalRate");
        int counterRateCol = TryGetCol("CounterRate");
        int avoidRateCol = TryGetCol("AvoidRate");

        int indexCol = TryGetCol("Index");
        int unitTypeCol = TryGetCol("UnitType");
        int nameCol = TryGetCol("Name");
        if (nameCol < 0) nameCol = TryGetCol("UnitName");
        if (nameCol < 0) nameCol = TryGetCol("DisplayName");

        int isEnemyCol = TryGetCol("IsEnemy");

        for (int lineIndex = DataStartLinePlayerIndex0Based; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line);
            if (fields.Count < 2)
            {
                continue;
            }

            string index = GetField(fields, indexCol >= 0 ? indexCol : 0);
            if (string.IsNullOrWhiteSpace(index))
            {
                continue;
            }

            string unitType = GetField(fields, unitTypeCol >= 0 ? unitTypeCol : 1);
            string name = nameCol >= 0 ? GetField(fields, nameCol) : string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = unitType;
            }

            bool isEnemyRow;
            if (isEnemyCol >= 0)
            {
                var raw = GetField(fields, isEnemyCol);
                isEnemyRow = raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                             raw.Equals("yes", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                isEnemyRow = IsEnemyUnitIndex(index);
            }

            int hp = ParseIntOrDefault(GetField(fields, hpCol), 1);
            int atk = ParseIntOrDefault(GetField(fields, atkCol), 0);
            int def = ParseIntOrDefault(GetField(fields, defCol), 0);
            int luck = ParseIntOrDefault(GetField(fields, luckCol), 0);

            float speed = ParseFloatPercentOrFloat(GetField(fields, speedCol), 0f);
            float criticalRate = ParseFloatPercentOrFloat(GetField(fields, criticalRateCol), 0f);
            float counterRate = ParseFloatPercentOrFloat(GetField(fields, counterRateCol), 0f);
            float avoidRate = ParseFloatPercentOrFloat(GetField(fields, avoidRateCol), 0f);

            var unit = new UnitData
            {
                Index = index,
                UnitType = unitType,
                Name = name,
                baseStats = new StatBlock(hp, atk, def, luck, speed, criticalRate, counterRate, avoidRate),
                IsEnemyRow = isEnemyRow
            };

            result.Add(unit);
        }

        return result;
    }

    public static List<EnemyData> LoadEnemyData(string csvText)
    {
        var result = new List<EnemyData>();
        if (string.IsNullOrWhiteSpace(csvText))
        {
            return result;
        }

        csvText = csvText.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = csvText.Split('\n');
        if (lines.Length <= DataStartLineEnemyIndex0Based)
        {
            return result;
        }

        var headerFields = ParseCsvLine(lines[0]);
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerFields.Count; i++)
        {
            var key = NormalizeHeader(headerFields[i]);
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            if (!headerIndex.ContainsKey(key))
            {
                headerIndex.Add(key, i);
            }
        }

        int TryGetCol(string headerName)
        {
            return headerIndex.TryGetValue(headerName, out var col) ? col : -1;
        }

        int hpCol = TryGetCol("HP");
        int atkCol = TryGetCol("Atk");
        int defCol = TryGetCol("DEF");
        if (defCol < 0) defCol = TryGetCol("Def");

        int luckCol = TryGetCol("Luck");

        int speedCol = TryGetCol("Speed");
        int criticalRateCol = TryGetCol("CriticalRate");
        int counterRateCol = TryGetCol("CounterRate");
        int avoidRateCol = TryGetCol("AvoidRate");

        int indexCol = TryGetCol("Index");
        int unitTypeCol = TryGetCol("UnitType");
        int nameCol = TryGetCol("Name");
        if (nameCol < 0) nameCol = TryGetCol("UnitName");
        if (nameCol < 0) nameCol = TryGetCol("DisplayName");

        int aiTypeCol = TryGetCol("AiType");
        if (aiTypeCol < 0) aiTypeCol = TryGetCol("AI");
        if (aiTypeCol < 0) aiTypeCol = TryGetCol("AIType");

        int isEnemyCol = TryGetCol("IsEnemy");

        for (int lineIndex = DataStartLineEnemyIndex0Based; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line);
            if (fields.Count < 2)
            {
                continue;
            }

            string index = GetField(fields, indexCol >= 0 ? indexCol : 0);
            if (string.IsNullOrWhiteSpace(index))
            {
                continue;
            }

            string unitType = GetField(fields, unitTypeCol >= 0 ? unitTypeCol : 1);
            string name = nameCol >= 0 ? GetField(fields, nameCol) : string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = unitType;
            }

            bool isEnemyRow;
            if (isEnemyCol >= 0)
            {
                var raw = GetField(fields, isEnemyCol);
                isEnemyRow = raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                             raw.Equals("yes", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                isEnemyRow = IsEnemyUnitIndex(index);
            }

            int hp = ParseIntOrDefault(GetField(fields, hpCol), 1);
            int atk = ParseIntOrDefault(GetField(fields, atkCol), 0);
            int def = ParseIntOrDefault(GetField(fields, defCol), 0);
            int luck = ParseIntOrDefault(GetField(fields, luckCol), 0);

            float speed = ParseFloatPercentOrFloat(GetField(fields, speedCol), 0f);
            float criticalRate = ParseFloatPercentOrFloat(GetField(fields, criticalRateCol), 0f);
            float counterRate = ParseFloatPercentOrFloat(GetField(fields, counterRateCol), 0f);
            float avoidRate = ParseFloatPercentOrFloat(GetField(fields, avoidRateCol), 0f);

            string aiTypeValue = aiTypeCol >= 0 ? GetField(fields, aiTypeCol) : string.Empty;
            if (string.IsNullOrEmpty(aiTypeValue) && fields.Count > 0)
            {
                aiTypeValue = GetField(fields, fields.Count - 1);
            }

            var enemy = new EnemyData
            {
                Index = index,
                UnitType = unitType,
                Name = name,
                baseStats = new StatBlock(hp, atk, def, luck, speed, criticalRate, counterRate, avoidRate),
                IsEnemyRow = isEnemyRow,
                //aiType = aiTypeValue
            };

            result.Add(enemy);
        }

        return result;
    }

    private static string NormalizeHeader(string raw)
    {
        if (raw == null) return string.Empty;
        raw = raw.Trim().Trim('\"');

        if (!string.IsNullOrEmpty(raw) && raw[0] == '\uFEFF')
        {
            raw = raw.Substring(1);
        }

        return raw;
    }

    private static string GetField(List<string> fields, int colIndex)
    {
        if (colIndex < 0 || colIndex >= fields.Count) return string.Empty;
        return fields[colIndex]?.Trim() ?? string.Empty;
    }

    private static int ParseIntOrDefault(string raw, int defaultValue)
    {
        raw = raw?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return intValue;
        }

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
        {
            return Mathf.RoundToInt(floatValue);
        }

        return defaultValue;
    }

    private static float ParseFloatPercentOrFloat(string raw, float defaultValue)
    {
        raw = raw?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        bool isPercent = raw.Contains("%");

        raw = raw.Replace("%", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        float value = float.Parse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
        return isPercent ? value / 100f : value;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        if (line == null)
        {
            return result;
        }

        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Length = 0;
                continue;
            }

            sb.Append(c);
        }

        result.Add(sb.ToString());
        return result;
    }
}
