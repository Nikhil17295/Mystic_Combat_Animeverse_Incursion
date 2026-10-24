namespace MysticCombat.Fighter
{
    /// <summary>
    /// Enumerates all possible states a fighter can be in.
    /// </summary>
    public enum FighterState
    {
        Idle,
        Walking,
        Jumping,
        Falling,
        Crouching,
        LightAttack,
        HeavyAttack,
        Skill,
        Ultimate,
        HitStun,
        KnockBack,
        Meditating,
        Exhausted
    }
}
