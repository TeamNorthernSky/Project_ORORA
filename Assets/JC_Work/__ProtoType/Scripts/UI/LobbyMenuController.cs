using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyMenuController : MonoBehaviour
{
    private const string Prefix = "BTN_Lobby_";

    [SerializeField] private GameObject _modalHQ;

    private void Awake()
    {
        BindLobbyButtons();
        BindModalCloseButtons();
    }

    private void BindLobbyButtons()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int matched = 0, missed = 0;
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            var n = btn.gameObject.name;
            if (!n.StartsWith(Prefix)) continue;
            var key = n.Substring(Prefix.Length);
            if (TryGetHandler(key, out var handler))
            {
                btn.onClick.AddListener(handler);
                matched++;
            }
            else
            {
                Debug.LogWarning($"[Lobby] 매칭 안 된 버튼: {n}");
                missed++;
            }
        }
        Debug.Log($"[Lobby] 버튼 매칭 {matched}건, 미매칭 {missed}건");
    }

    private bool TryGetHandler(string key, out UnityAction handler)
    {
        switch (key)
        {
            case "HQ":           handler = OnClickHQ;          return true;
            case "Broadcast":    handler = OnClickBroadcast;   return true;
            case "Enhancement":  handler = OnClickEnhancement; return true;
            case "Research":     handler = OnClickResearch;    return true;
            case "Recruitment":  handler = OnClickRecruitment; return true;
            case "Go":           handler = OnClickGo;          return true;
            case "Exit":         handler = OnClickExit;        return true;
            case "EndTurn":      handler = OnClickEndTurn;     return true;
            case "member1":      handler = OnClickMember1;     return true;
            case "member2":      handler = OnClickMember2;     return true;
            case "member3":      handler = OnClickMember3;     return true;
            case "member4":      handler = OnClickMember4;     return true;
            case "replace":      handler = OnClickReplace;     return true;
            default:             handler = null;               return false;
        }
    }

    private void BindModalCloseButtons()
    {
        if (_modalHQ == null) return;
        var closes = _modalHQ.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < closes.Length; i++)
        {
            if (closes[i].gameObject.name == "CloseButton")
                closes[i].onClick.AddListener(CloseModalHQ);
        }
    }

    public void OnClickHQ()
    {
        Debug.Log("[Lobby] HQ");
        if (_modalHQ != null) _modalHQ.SetActive(true);
    }
    public void OnClickBroadcast()    { Debug.Log("[Lobby] Broadcast"); }
    public void OnClickEnhancement()  { Debug.Log("[Lobby] Enhancement"); }
    public void OnClickResearch()     { Debug.Log("[Lobby] Research"); }
    public void OnClickRecruitment()  { Debug.Log("[Lobby] Recruitment"); }
    public void OnClickGo()           { Debug.Log("[Lobby] Go"); }
    public void OnClickExit()         { Debug.Log("[Lobby] Exit"); }
    public void OnClickEndTurn()      { Debug.Log("[Lobby] EndTurn"); }
    public void OnClickMember1()      { Debug.Log("[Lobby] Member1"); }
    public void OnClickMember2()      { Debug.Log("[Lobby] Member2"); }
    public void OnClickMember3()      { Debug.Log("[Lobby] Member3"); }
    public void OnClickMember4()      { Debug.Log("[Lobby] Member4"); }
    public void OnClickReplace()      { Debug.Log("[Lobby] Replace"); }

    public void CloseModalHQ()
    {
        if (_modalHQ != null) _modalHQ.SetActive(false);
    }
}
