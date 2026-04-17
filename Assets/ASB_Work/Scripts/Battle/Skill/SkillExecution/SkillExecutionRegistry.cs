using System.Collections.Generic;

/// <summary>
/// skillIndex → 커스텀 핸들러. 등록되지 않은 인덱스는 BattleManager 기본 실행을 사용합니다.
/// </summary>
public static class SkillExecutionRegistry
{
    /// <summary>데모/예시용 — CSV와 겹치지 않게 큰 번호를 사용합니다.</summary>
    public static class DemoSkillIndices
    {
        public const int DefaultDamageExample = 99001;
        public const int DefaultHealExample = 99002;
        public const int BleedStrikeExample = 99003;
    }

    private static readonly Dictionary<int, ISkillEffectHandler> Handlers = new Dictionary<int, ISkillEffectHandler>();
    private static bool s_initialized;

    static SkillExecutionRegistry()
    {
        EnsureInitialized();
    }

    /// <summary>외부에서 명시적으로 한 번 더 부를 수 있도록 공개(중복은 무시).</summary>
    public static void EnsureInitialized()
    {
        if (s_initialized)
        {
            return;
        }

        s_initialized = true;
        Register(DemoSkillIndices.DefaultDamageExample, new DefaultDamageSkillHandler());
        Register(DemoSkillIndices.DefaultHealExample, new DefaultHealSkillHandler());
        Register(DemoSkillIndices.BleedStrikeExample, new BleedStrikeSkillHandler());
    }

    public static bool TryGetHandler(int skillIndex, out ISkillEffectHandler handler)
    {
        EnsureInitialized();
        return Handlers.TryGetValue(skillIndex, out handler);
    }

    private static void Register(int skillIndex, ISkillEffectHandler handler)
    {
        if (handler == null)
        {
            return;
        }

        if (Handlers.ContainsKey(skillIndex))
        {
            UnityEngine.Debug.LogWarning($"[SkillExecutionRegistry] skillIndex {skillIndex} 중복 등록 무시.");
            return;
        }

        Handlers.Add(skillIndex, handler);
    }
}
