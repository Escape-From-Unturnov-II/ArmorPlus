namespace PvPRework
{
    public class ArmorClass
    {
        public float Tier { get; set; } // defines the armor tier for the pen calculation
        public float Armor { get; set; } // the armor rating for this class (1-0, this is the unturned armor multiplier where 0 would be 100% Damage reduction)

        public float PercentForNormalDamage { get; set; }  //damage required to do DamageMultiplierNormal blow this DamageMultiplierMin is used
        public float PercentForMaxDamage { get; set; }  //damage required to do 100% damage
        public float DamageMultiplierMin { get; set; } //damage multiplier for minimal damage (0-1)
        public float DamageMultiplierNormal { get; set; } //damage multiplier normal damage (0-1)

        public int MinArmorDamage { get; set; } //min damage done to armor (1 = 1%)
        public int MaxArmorDamage { get; set; } //max damage done to armor (1 = 1%)
        public float DamageToDamageArmorMin { get; set; } //damage required to do MinArmorDamage
        public float DamageToDamageArmorMax { get; set; } //damage required to do MaxArmorDamage

        public float StopDamageMulti { get; set; } //damage multiplier when not penetrating (0-1)
        public float PenLossMulti; //the reduction in penetration power when penetrating this armor, this is additional to the reduction by pen chance (0.1 would result in 10% penetration power reduction)

        public ArmorClass() {
            
        }
    }
}
