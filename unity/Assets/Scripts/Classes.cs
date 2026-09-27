using System.Collections.Generic;
using UnityEngine;

public enum Mobility { Flash, Teleport, Leap, DoubleJump }

// A playable class: art, skill names, MP costs, cooldowns and stat growth.
// Skill slots everywhere: 0 = J (basic), 1 = K, 2 = L, 3 = U, 4 = double-jump mobility.
public class ClassDef
{
    public string id, name, role, sheet, ghost, icons;
    public string[] desc;
    public string[] skill;
    public int[] mp;
    public float[] cd;          // J, K, L, U
    public Mobility mob;
    public int hpBase, hpLv, mpBase, mpLv;
    public Sprite Icon(int slot)
    {
        if (icons == null)   // night lord: the original icon sheets
            return slot < 4 ? Atlas.Sheets["icons"].frames[new[] { 0, 1, 2, 3 }[slot]] : Atlas.Named("icons2", "flash");
        return Atlas.Named(icons, new[] { "j", "k", "l", "u", "sp" }[slot]);
    }
    public string Portrait { get { return "portrait_pc_" + id; } }
}

public static class Classes
{
    public static readonly List<ClassDef> All = new List<ClassDef>
    {
        new ClassDef { id = "hero", name = "히어로", role = "전사", sheet = "pc_hero", ghost = "pc_hero_ghost", icons = "icons_hero",
            desc = new[] { "무거운 갑옷과 거대한 검.", "적진을 뚫고 돌진하며", "대지를 둘로 가른다." },
            skill = new[] { "슬래시", "러시", "드래곤 퓨리", "월드리버", "도약" }, mp = new[] { 0, 12, 20, 22, 2 }, cd = new[] { 0.42f, 2.5f, 6f, 7f },
            mob = Mobility.Leap, hpBase = 60, hpLv = 28, mpBase = 16, mpLv = 8 },
        new ClassDef { id = "archmage", name = "아크메이지", role = "마법사", sheet = "pc_archmage", ghost = "pc_archmage_ghost", icons = "icons_archmage",
            desc = new[] { "멀리서 얼음과 불을 쏜다.", "블리자드, 메테오,", "그리고 텔레포트." },
            skill = new[] { "아이스 볼트", "블리자드", "메테오", "아이스 스트라이크", "텔레포트" }, mp = new[] { 0, 14, 24, 10, 3 }, cd = new[] { 0.45f, 4f, 7f, 3f },
            mob = Mobility.Teleport, hpBase = 45, hpLv = 14, mpBase = 30, mpLv = 22 },
        new ClassDef { id = "bishop", name = "비숍", role = "성직자", sheet = "pc_bishop", ghost = "pc_bishop_ghost", icons = "icons_bishop",
            desc = new[] { "적을 벌하고 치유하는", "성스러운 빛. 제네시스의", "빛기둥을 내린다." },
            skill = new[] { "홀리 애로우", "엔젤레이", "제네시스", "힐", "텔레포트" }, mp = new[] { 0, 10, 22, 12, 3 }, cd = new[] { 0.45f, 2.5f, 7f, 6f },
            mob = Mobility.Teleport, hpBase = 48, hpLv = 16, mpBase = 30, mpLv = 20 },
        new ClassDef { id = "bowmaster", name = "보우마스터", role = "궁수", sheet = "pc_bowmaster", ghost = "pc_bowmaster_ghost", icons = "icons_bowmaster",
            desc = new[] { "장궁의 명사수.", "파워 샷, 폭발 화살,", "그리고 허리케인." },
            skill = new[] { "애로우", "파워 샷", "허리케인", "애로우 봄", "더블 점프" }, mp = new[] { 0, 12, 18, 8, 2 }, cd = new[] { 0.38f, 3f, 7f, 1.5f },
            mob = Mobility.DoubleJump, hpBase = 50, hpLv = 20, mpBase = 20, mpLv = 12 },
        new ClassDef { id = "nightlord", name = "나이트로드", role = "도적", sheet = "ninja", ghost = "ninja_ghost", icons = null,
            desc = new[] { "표창과 그림자,", "순간이동 암살.", "분신과 함께 싸운다." },
            skill = new[] { "트리플 스로우", "어벤져", "어쌔시네이트", "쉐도우 파트너", "플래시 점프" }, mp = new[] { 0, 10, 16, 20, 2 }, cd = new[] { 0.36f, 4f, 6f, 30f },
            mob = Mobility.Flash, hpBase = 50, hpLv = 22, mpBase = 20, mpLv = 14 },
    };
    public static ClassDef Get(string id) { foreach (var c in All) if (c.id == id) return c; return All[4]; }
    public static ClassDef Cur { get { return Get(Stats.D.cls); } }
    public static bool Available(ClassDef c) { return Atlas.Sheets.ContainsKey(c.sheet); }
}
