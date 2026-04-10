using System;

namespace AutobattlerSample.Data
{
    [Serializable]
    public class ActionInstance
    {
        public ActionData Data;
        public int CurrentCooldown;
        public int Priority; // lower = used first

        public bool IsReady => CurrentCooldown <= 0;
        public string DisplayName => Data != null ? Data.DisplayName : "Action";
        public ActionType Type => Data != null ? Data.Type : ActionType.Attack;
        public int Amount => Data != null ? Data.Amount : 0;
        public int MaxCooldown => Data != null ? Data.Cooldown : 1;

        public ActionInstance() { }

        public ActionInstance(ActionData data, int priority)
        {
            Data = data;
            Priority = priority;
            CurrentCooldown = 0;
        }

        public void TickCooldown()
        {
            if (CurrentCooldown > 0)
                CurrentCooldown--;
        }

        public void StartCooldown()
        {
            CurrentCooldown = Data != null ? Data.Cooldown : 1;
        }

        public void Haste(int amount)
        {
            CurrentCooldown = Math.Max(0, CurrentCooldown - amount);
        }

        public ActionInstance Clone()
        {
            return new ActionInstance
            {
                Data = Data?.Clone(),
                CurrentCooldown = CurrentCooldown,
                Priority = Priority
            };
        }
    }
}

