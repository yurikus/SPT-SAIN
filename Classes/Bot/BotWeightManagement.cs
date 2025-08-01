using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Components;
using SAIN.Preset.GlobalSettings;
using System.Collections.Generic;
using FloatFunc = GClass828<float>;

namespace SAIN.SAINComponent.Classes
{
    public class BotWeightManagement : BotComponentClassBase
    {
        public BotWeightManagement(BotComponent sain) : base(sain)
        {
            CanEverTick = false;
        }

        public override void Init()
        {
            if (GlobalSettingsClass.Instance.General.BOT_INTERTIA_TOGGLE)
            {
                getSlots();
                Traverse.Create(Player.InventoryController.Inventory).Field<FloatFunc>("TotalWeight").Value = new FloatFunc(getBotTotalWeight);
                Player.Physical.EncumberDisabled = false;
            }
            base.Init();
        }

        private void getSlots()
        {
            _slots.Clear();
            foreach (var slot in _botEquipmentSlots)
            {
                _slots.Add(Player.Equipment.GetSlot(slot));
            }
        }

        private float getBotTotalWeight()
        {
            float result = InventoryEquipment.smethod_1(_slots);
            _slots.Clear();
            // Logger.LogWarning(result);
            return result;
        }

        private readonly List<Slot> _slots = new();

        public static readonly EquipmentSlot[] _botEquipmentSlots =
        [
            EquipmentSlot.Backpack,
            EquipmentSlot.TacticalVest,
            EquipmentSlot.ArmorVest,
            EquipmentSlot.Eyewear,
            EquipmentSlot.FaceCover,
            EquipmentSlot.Headwear,
            EquipmentSlot.Earpiece,
            EquipmentSlot.FirstPrimaryWeapon,
            EquipmentSlot.SecondPrimaryWeapon,
            EquipmentSlot.Holster,
            EquipmentSlot.Pockets,
        ];
    }
}
