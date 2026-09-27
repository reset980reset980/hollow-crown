using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int level = 1, exp, hp = -1, mp = -1, meso = 50;
    public int red = 5, blue = 3, cap, root, crown, starTier;
    public int quest, qstate, qcount;       // quest chain index, 0 not started / 1 active / 2 ready, kill counter
    public string map = "town", cls = "nightlord";
    public bool bossDown, seenIntro;
}

public class QuestDef
{
    public string title, kind, target;      // kind: kill | collect | boss
    public int need, rExp, rMeso, rRed, rBlue;
    public string[] offer, active, done;
}

// Character progression, inventory, quests and saving (MapleStory-style numbers: small and growing).
public static class Stats
{
    public static SaveData D = new SaveData();
    public static string[] SkillName { get { return Classes.Cur.skill; } }
    public static readonly int[] SkillLevel = { 1, 6, 8, 4, 2 };
    public static int[] SkillMp { get { return Classes.Cur.mp; } }
    static readonly int[] StarAtk = { 0, 8, 20 };
    public static readonly string[] StarName = { "없음", "힘", "영광" };   // attack runes (saved as starTier)

    public static int Level { get { return D.level; } }
    public static int MaxHp { get { var c = Classes.Cur; return c.hpBase + c.hpLv * D.level; } }
    public static int MaxMp { get { var c = Classes.Cur; return c.mpBase + c.mpLv * D.level; } }
    public static int Atk { get { return 10 + 4 * D.level + StarAtk[D.starTier]; } }
    public static int ExpNeed(int lvl) { return Mathf.RoundToInt(20 * Mathf.Pow(lvl, 1.7f)); }
    public static bool Unlocked(int skill) { return D.level >= SkillLevel[skill]; }

    public static readonly QuestDef[] Quests =
    {
        new QuestDef { title = "버섯 숲의 쉘백", kind = "kill", target = "shellback", need = 8, rExp = 60, rMeso = 300, rRed = 10,
            offer = new[] { "오, 모험가로군. 마침 잘 왔네.", "마을 동쪽 버섯 숲에서 쉘백들이 기어 나왔네. 우리 울타리를 갉아서 산산조각 내고 있지.", "놈들을 8마리 처치해 주겠나?" },
            active = new[] { "버섯 숲은 동쪽 포탈 너머에 있네. 포탈 위에서 ↑를 누르면 이동할 수 있지." },
            done = new[] { "잘했네! 울타리도 고마워할 걸세. 이 포션들을 받게, 곧 필요할 테니." } },
        new QuestDef { title = "캐플링의 갓", kind = "collect", target = "cap", need = 8, rExp = 220, rMeso = 800, rBlue = 8,
            offer = new[] { "캐플링들이 들떠 있네. 버섯이 괜히 뛰어다닐 리가 없지...", "캐플링 갓을 8개 가져다주게. 모닥불 곁에서 조사해 보겠네." },
            active = new[] { "캐플링은 버섯 숲의 높은 발판에 사네. ↑를 눌러 밧줄을 타고 오르게." },
            done = new[] { "이 갓들... 왕가의 문장이 새겨져 있군. 무언가가 놈들을 조종하고 있어." } },
        new QuestDef { title = "뿌리 깊은 문제", kind = "kill", target = "stumpy", need = 12, rExp = 520, rMeso = 1500, rRed = 10, rBlue = 10,
            offer = new[] { "버섯 숲 너머에는 할로우 딥이 있네. 그곳에선 오래된 그루터기마저 깨어났지.", "놈들의 수를 줄여 주게. 스텀피 12마리를 처치하게." },
            active = new[] { "할로우 딥은 버섯 숲 동쪽에 있네. 위스프를 조심하게." },
            done = new[] { "자네도 강해졌군. 이제 진실을 말해 줄 수 있겠어..." } },
        new QuestDef { title = "왕관을 쓴 자", kind = "boss", target = "king", need = 1, rExp = 3000, rMeso = 8000,
            offer = new[] { "왕의 공터에서 킹 슈룸이 깨어났네. 숲을 어지럽히는 건 바로 그놈이야.", "이제 공터의 봉인이 자네에게 열릴 걸세. 놈의 통치를 끝내게, 모험가여." },
            active = new[] { "왕의 공터는 할로우 딥의 가장 동쪽 끝에 있네. 놈은 세 단계로 싸우니 바닥에 뜨는 표식을 잘 보게." },
            done = new[] { "버섯왕의 왕관이로군! 자네 덕분에 크라운할로우는 안전하네.", "자네야말로 숲의 진정한 영웅일세." } },
    };
    public static QuestDef Cur { get { return D.quest < Quests.Length ? Quests[D.quest] : null; } }
    public static int QuestProgress
    {
        get
        {
            var q = Cur; if (q == null) return 0;
            if (q.kind == "collect") return Mathf.Min(q.need, Item(q.target));
            return D.qcount;
        }
    }

    public static int Item(string id)
    {
        switch (id) { case "red": return D.red; case "blue": return D.blue; case "cap": return D.cap; case "root": return D.root; case "crown": return D.crown; }
        return 0;
    }
    public static void AddItem(string id, int n)
    {
        switch (id) { case "red": D.red += n; break; case "blue": D.blue += n; break; case "cap": D.cap += n; break; case "root": D.root += n; break; case "crown": D.crown += n; break; }
        if (Cur != null && D.qstate == 1 && Cur.kind == "collect" && Cur.target == id) QuestTick(false);
    }

    public static void GainExp(int n)
    {
        var G = Game.I;
        D.exp += n;
        G.hud.Gain("경험치", n, "gold");
        while (D.exp >= ExpNeed(D.level) && D.level < 30)
        {
            D.exp -= ExpNeed(D.level);
            D.level++;
            D.hp = MaxHp; D.mp = MaxMp;
            G.OnLevelUp();
        }
    }

    public static void OnKill(string mob)
    {
        var q = Cur;
        if (q == null || D.qstate != 1) return;
        if ((q.kind == "kill" || q.kind == "boss") && q.target == mob) { D.qcount++; QuestTick(true); }
    }
    static void QuestTick(bool announce)
    {
        var q = Cur; var G = Game.I;
        int p = QuestProgress;
        if (announce || q.kind == "collect") G.hud.Chat(q.title + " " + Mathf.Min(p, q.need) + "/" + q.need, "green", 2.5f);
        if (p >= q.need && D.qstate == 1)
        {
            D.qstate = 2;
            G.hud.Chat("퀘스트 완료! 장로 로완에게 돌아가세요.", "gold");
            G.hud.Toast("퀘스트 완료!", "goldBig");
            Sfx.Play("quest");
        }
    }

    public static void TurnIn()
    {
        var q = Cur; var G = Game.I;
        if (q.kind == "collect") AddItem(q.target, -q.need);
        D.meso += q.rMeso; if (q.rRed > 0) D.red += q.rRed; if (q.rBlue > 0) D.blue += q.rBlue;
        G.hud.Gain("메소", q.rMeso, "white");
        if (q.rRed > 0) G.hud.Chat("+" + q.rRed + " 빨간 포션", "white", 3f);
        if (q.rBlue > 0) G.hud.Chat("+" + q.rBlue + " 파란 포션", "white", 3f);
        D.quest++; D.qstate = 0; D.qcount = 0;
        GainExp(q.rExp);
        Sfx.Play("quest");
        Save();
    }

    // ---------------------------------------------------------------- saving
    const string KEY = "hollowcrown_save_v1";
    public static bool HasSave { get { return PlayerPrefs.HasKey(KEY); } }
    public static void Save()
    {
        if (Game.I != null && Game.I.capture) return;
        PlayerPrefs.SetString(KEY, JsonUtility.ToJson(D)); PlayerPrefs.Save();
    }
    public static void Load()
    {
        D = HasSave ? JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(KEY)) : new SaveData();
        if (D.hp < 0) D.hp = MaxHp;
        if (D.mp < 0) D.mp = MaxMp;
    }
    public static void NewGame(string cls) { D = new SaveData { cls = cls }; D.hp = MaxHp; D.mp = MaxMp; Save(); }
}
