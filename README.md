# ArmorPlus

Completely reworked Armor-System (based on penetration chance not just damage reduction).

Uses the Escape from Tarkov Armor-Tiers and [Penetration logic](https://www.desmos.com/calculator/m8cmsfokkl?lang=en).

Added new HitZones for arms, legs, tomach and face.

Fixes masks to actually protect the player.
Vests can protect parts of the arms and legs and you can chose wether they should protect the stomach.
It can be chosen if Hats should protect the face.

The System is fully customizable and is a perfect addition to all PvP focused Servers.

You can select armor damage depending on damage and armor tier.
If the server uses durability vanilla armor damage will be reverted to avoid double armor damage

Allows bullets to break player bones based on chance. (based on https://github.com/IcePlugins/BoneHurtingBullets)

All Calculations are shown in this [Excell Sheet](https://docs.google.com/spreadsheets/d/1Aq71OPWoRFZNGPXU-u45SQgkarJMai6a/edit?usp=sharing&ouid=104005208794763187898&rtpof=true&sd=true), this also allows to easily check penValues and armorClasses definitions.

## Settings
| Name   |      Description      |
|----------|-------------|
|Version:|shows the actual version of the plugin (this field will be automatically generated)|
|Debug:|shows debug infos in the server console (shows damge done / penchances and so on)|
|BreakLegs: |if bone breaking bullets should be enabled|

## BetterArmor
| Name   |      Description      |
|----------|-------------|
|Enabled:|if the new Armor system should be used (this is required for the mask fix, VestExtensions, HatExtensions, ArmorClasses as well as BetterHitZones)|
|UseArmorClasses:|if the armor should work as in vanilla or with the new penetration chances|
|ArmorDamageMultiplierOnPen:|multiplier used for damage done to armor when penetrating armor|
|PenDamgeDelta:|used to reduce pendamage loss with penetration chance (1-0 where 0 would equal to no reduction on any penchance and 1 would be 50% penetration chance = 50% pendamage loss)|

## BetterHitZones
This includes the fllowing hitzone: stomach, face, multiple for legs and arms
| Name   |      Description      |
|----------|-------------|
|Enabled:|if the new hitzones should be used|
|HatsProtectFace:|if all hats should protect the face by default (every exeption hass to be specified as HatExtension)|
|VestsProtectStomach:|if all bests should protect the stomach by default (every exeption hass to be specified as VestExtension)|

## HatExtensions
| Name   |      Description      |
|----------|-------------|
|Id:|id of the clothing|
|Name:|The name of the item (this field will be automatically generated)|
|ProtectFace:|if this hat should protect the face|
|ArmorFace:|Vanilla armor rating for the faceshield from (0-1 where 1 is no armor)|

```xml
    <HatExtension>
      <Id>1525</Id>
      <Name>Military_Helmet_Spec_Ops</Name>
      <ProtectFace>true</ProtectFace>
      <ArmorFace>0.85</ArmorFace>
    </HatExtension>
```

## VestExtensions
Allows vests to also protect legs or arms (this can also be used with vanila armor logic)
| Name   |      Description      |
|----------|-------------|
|Id:|id of the clothing|
|Name:|The name of the item (this field will be automatically generated)|
|ProtectStomach:|if this vest should protect the stomach|
|ShoulderPlateLength:|0 - 0.9 (0 is disabled, 0.23 is only shoulder , 0.4 is upper arm, 0.9 is full arm)|
|ArmorShoulderPlate:| vanilla armor rating for shoulders / arms from (0-1 where 1 is no armor)|
|ThighPlateLength:|0 - 0.9 (0 is disabled, 0.3 is full thigh, 0.9 is full leg)|
|ArmorThighPlate:| vanilla armor rating for thighs / legs from (0-1 where 1 is no armor)|

```xml
    <VestExtension>
      <Id>1169</Id>
      <Name>Vest_Spec_Ops</Name>
      <ProtectStomach>true</ProtectStomach>
      <ShoulderPlateLength>0.4</ShoulderPlateLength>
      <ArmorShoulderPlate>0.45</ArmorShoulderPlate>
      <ThighPlateLength>0</ThighPlateLength>
      <ArmorThighPlate>1</ArmorThighPlate>
    </VestExtension>
```

## ArmorClass
Allows to define ArmorClasses by armor value (this is the "Armor" value provided in the clothing.dat) 

**ArmorClasses need to start at armor 1 and go down from there and tier needs to start at 0 and go up from there!**

When a armor value is not defined it will be calculated by taking the mean from the class below and above the armor value, or take max class if max
Penetration calculations are based on Tarkov logic: https://www.desmos.com/calculator/m8cmsfokkl?lang=en
| Name   |      Description      |
|----------|-------------|
|Armor:|the armor value defining this class (the armor value required of the clothin item to fall in this class)|
|Tier:|defines the armor tier for the pen calculation (0-10)|
|||
|PercentForNormalDamage:|damage required to do DamageMultiplierNormal, blow this DamageMultiplierMin is used|
|PercentForMaxDamage:|damage required to do 100% damage|
|DamageMultiplierMin:|damage multiplier for minimal damage (0-1)|
|DamageMultiplierNormal:|damage multiplier normal damage (0-1)|
|||
|MinArmorDamage:|min damage done to armor when not penetrating (1 = 1%, when penetrating ArmorDamageMultiplierOnPen is used)|
|MaxArmorDamage:|max damage done to armor when not penetrating (1 = 1%, when penetrating ArmorDamageMultiplierOnPen is used)|
|DamageToDamageArmorMin:|damage required to do MinArmorDamage, below this no damage is done|
|DamageToDamageArmorMax:|damage required to do MaxArmorDamage, below this the mean damage between MinArmorDamage and MaxArmorDamage is calculated|
|||
|StopDamageMulti:|damage multiplier when not penetrating (0-1), this simulates the hit you get when shot on armor and should be very small|
|PenLossMulti:|the reduction in penetration power when penetrating this armor, this is additional to the reduction by pen chance (0.1 would result in a 10% additional penetration power reduction, see PenDamgeDelta)|

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

## GunExtension
| Name   |      Description      |
|----------|-------------|
|Id:|the id of the gun|
|Name:|The name of the item (this field will be automatically generated)|
|Penetration:|the penetration stat of the gun|

```xml
    <GunExtension>
      <Id>107</Id>
      <Name>Ace</Name>
      <Penetration>17</Penetration>
    </GunExtension>
```

## BulletLimbDamageChance
Allowes to define bone break chances per Limb, the chance scales with damage.

| Name   |      Description      |
|----------|-------------|
|Limb:|the limb that needs to be hit|
| BreakChanceDamageMin:|the min chance to break legs|
|BreakChanceMax:|the max chance to break legs|
|BreakChanceDamageMin:|min damage required to have a chance to break legs|
|BreakChanceDamageMax:|damage required to have the max chance to break legs|

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
