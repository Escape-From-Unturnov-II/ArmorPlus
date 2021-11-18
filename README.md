# PVP Rework

Completely reworked the Armor-System (based on penetration chance not just damage decrease)
Allow bullets to break player's bones based on chance. (based on https://github.com/IcePlugins/BoneHurtingBullets)
Fixes masks to actually protect player.

## Settings
- Debug: shows debug infos in the server console
- BreakLegs: if bone breaking bullets shoudl be enabled
- BetterArmor: if the new Armor system should be used (this is required for vests to protect arms or legs)
- UseArmorClasses: if the armor shoudl work as vanilla or with the new penetration chances
- HasDuribility: set to true if the server uses durability
- ArmorDamageMultiplierOnPen: multiplier used for damage done to armor when penetrating armor

## vestsProtectingArms / Legs
Allows vests to also protect legs or arms

- Key: id of the clothing
- Value: the armor multiplier used for arms or legs

```xml
    <Vest>
      <Key>1169</Key>
      <Value>0.75</Value>
    </Vest>
```

## ArmorClass
Allows to define ArmorClasses by armor value (this is the "Armor" value provided in the clothing.dat), ArmorClasses need to **start at 1 and go down from there**!
When a armor value is not defined it will be calculated by taking the mean from the class below and above the armor value, or take max class if above max
Penetration calculations are based on Tarkov logic: https://www.desmos.com/calculator/m8cmsfokkl?lang=en

- Armor: the armor value defining this class (the armor value required of the clothin item to fall in this class)
- Tier: defines the armor tier for the pen calculation (0-10)
- 
- PercentForNormalDamage: damage required to do DamageMultiplierNormal blow this DamageMultiplierMin is used
- PercentForMaxDamage: damage required to do 100% damage
- DamageMultiplierMin: damage multiplier for minimal damage (0-1)
- DamageMultiplierNormal: damage multiplier normal damage (0-1)
- 
- MinArmorDamage: min damage done to armor when not penetrating (1 = 1%, on penn ArmorDamageMultiplierOnPen is used)
- MaxArmorDamage: max damage done to armor when not penetrating (1 = 1%, on penn ArmorDamageMultiplierOnPen is used)
- DamageToDamageArmorMin: damage required to do MinArmorDamage, below this no damage is done
- DamageToDamageArmorMax: damage required to do MaxArmorDamage, below this the mean damage is between MinArmorDamage and MaxArmorDamage is calculated
- 
- StopDamageMulti: damage multiplier when not penetrating (0-1)
- PennLossMulti: the reduction in penetration power when penetrating this armor, this is additional to the reduction by pen chance (0.1 would result in a 10% additional penetration power reduction)

```xml
    <ArmorClass>
      <Tier>4</Tier>
      <Armor>0.65</Armor>
      <PercentForNormalDamage>20</PercentForNormalDamage>
      <PercentForMaxDamage>90</PercentForMaxDamage>
      <DamageMultiplierMin>0.4</DamageMultiplierMin>
      <DamageMultiplierNormal>0.8</DamageMultiplierNormal>
      <MinArmorDamage>1</MinArmorDamage>
      <MaxArmorDamage>2</MaxArmorDamage>
      <DamageToDamageArmorMin>20</DamageToDamageArmorMin>
      <DamageToDamageArmorMax>40</DamageToDamageArmorMax>
      <StopDamageMulti>0.02</StopDamageMulti>
    </ArmorClass>
```

## gunPenValues:

- Key: the id of the gun
- Value: the penetration stat

```xml
<Gun>
	<Key>107</Key>
    <Value>20</Value>
</Gun>
```

## BulletLimbDamageChance
Allowes to define a bone break chance per Limb, the chance scales with damage

- Limb: the limb that needs to be hit
- BreakChanceDamageMin: the min chance to break legs
- BreakChanceMax: the max chance to break legs
- BreakChanceDamageMin: min damage required to have a chance to break legs
- BreakChanceDamageMax: damage required to have the max chance to break legs

(This allows a 95% chance when shot with highcall sniper and does avoid breaking legs with paintball gun)
```xml
<BulletLimbDamageChance>
    <Limb>LEG</Limb>
    <BreakChanceMin>10</BreakChanceMin>
    <BreakChanceMax>95</BreakChanceMax>
	  <BreakChanceDamageMin>10</BreakChanceDamageMin>
    <BreakChanceDamageMax>60</BreakChanceDamageMax>
</BulletLimbDamageChance>
```
