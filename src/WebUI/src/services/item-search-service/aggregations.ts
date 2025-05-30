import type { ItemFlat } from '~/models/item'
import type { AggregationConfig } from '~/models/item-search'

import {
  ItemFieldCompareRule,
  ItemFieldFormat,
  ItemType,
  WeaponClass,
} from '~/models/item'
import { AggregationView } from '~/models/item-search'

const size = 1000

export const aggregationsConfig: AggregationConfig = {
  culture: {
    chosen_filters_on_top: false,
    conjunction: false,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },
  flags: {
    chosen_filters_on_top: false,
    conjunction: false,
    format: ItemFieldFormat.List,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
    width: 160,
  },
  id: {
    chosen_filters_on_top: false,
    conjunction: false,
    hidden: true,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },
  modId: {
    chosen_filters_on_top: false,
    conjunction: false,
    hidden: true,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },
  new: {
    chosen_filters_on_top: false,
    conjunction: false,
    hidden: true,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },
  price: {
    compareRule: ItemFieldCompareRule.Less,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
    width: 200,
  },
  requirement: {
    compareRule: ItemFieldCompareRule.Less,
    format: ItemFieldFormat.Requirement,
    size,
    view: AggregationView.Range,
  },
  tier: {
    chosen_filters_on_top: false,
    compareRule: ItemFieldCompareRule.Bigger,
    conjunction: false,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  type: {
    chosen_filters_on_top: false,
    conjunction: false,
    size,
    sort: 'term',
    view: AggregationView.Radio,
  },
  upkeep: {
    compareRule: ItemFieldCompareRule.Less,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  weight: {
    compareRule: ItemFieldCompareRule.Less,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },

  // Armor
  armArmor: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  armorFamilyType: {
    chosen_filters_on_top: false,
    conjunction: false,
    format: ItemFieldFormat.List,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },
  armorMaterialType: {
    chosen_filters_on_top: false,
    conjunction: false,
    format: ItemFieldFormat.List,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },
  bodyArmor: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  headArmor: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  legArmor: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },

  // Mount
  bodyLength: {
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  chargeDamage: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  hitPoints: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  maneuver: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  mountFamilyType: {
    chosen_filters_on_top: false,
    conjunction: false,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },
  speed: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },

  // Mount armor
  mountArmor: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  mountArmorFamilyType: {
    chosen_filters_on_top: false,
    conjunction: false,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },

  // Weapon
  handling: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  length: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  swingDamage: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Damage,
    size,
    view: AggregationView.Range,
  },
  swingDamageType: {
    chosen_filters_on_top: false,
    conjunction: false,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },
  swingSpeed: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  thrustDamage: {
    compareRule: ItemFieldCompareRule.Bigger,
    conjunction: false,
    format: ItemFieldFormat.Damage,
    hide_zero_doc_count: true,
    size,
    view: AggregationView.Range,
  },
  thrustDamageType: {
    chosen_filters_on_top: false,
    conjunction: false,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },
  thrustSpeed: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  weaponClass: {
    chosen_filters_on_top: false,
    conjunction: false,
    size,
    sort: 'term',
    view: AggregationView.Radio,
  },
  weaponUsage: {
    chosen_filters_on_top: false,
    conjunction: false,
    hidden: true,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },

  // Throw/Bow/Xbow
  accuracy: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  missileSpeed: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },

  // Bow/Xbow
  aimSpeed: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  reloadSpeed: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },

  // Arrows/Bolts/Thrown
  damage: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Damage,
    size,
    view: AggregationView.Range,
  },
  damageType: {
    chosen_filters_on_top: false,
    conjunction: false,
    size,
    sort: 'term',
    view: AggregationView.Checkbox,
  },
  stackAmount: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  stackWeight: {
    compareRule: ItemFieldCompareRule.Less,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },

  // SHIELD
  shieldArmor: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  shieldDurability: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
  shieldSpeed: {
    compareRule: ItemFieldCompareRule.Bigger,
    format: ItemFieldFormat.Number,
    size,
    view: AggregationView.Range,
  },
}

export const aggregationsKeysByItemType: Partial<Record<ItemType, Array<keyof ItemFlat>>>
  = {
    [ItemType.BodyArmor]: [
      'armorFamilyType',
      'culture',
      'flags',
      'armorMaterialType',
      'weight',
      'bodyArmor',
      'armArmor',
      'legArmor',
      'upkeep',
      'price',
    ],
    [ItemType.HandArmor]: [
      'culture',
      'flags',
      'armorMaterialType',
      'weight',
      'armArmor',
      'upkeep',
      'price',
    ],
    [ItemType.HeadArmor]: [
      'culture',
      'flags',
      'armorMaterialType',
      'weight',
      'headArmor',
      'upkeep',
      'price',
    ],
    [ItemType.LegArmor]: [
      'armorFamilyType',
      'culture',
      'armorMaterialType',
      'weight',
      'legArmor',
      'upkeep',
      'price',
    ],
    [ItemType.Mount]: [
      'culture',
      'mountFamilyType',
      'bodyLength',
      'chargeDamage',
      'maneuver',
      'speed',
      'hitPoints',
      'upkeep',
      'price',
    ],
    [ItemType.MountHarness]: [
      'culture',
      'mountArmorFamilyType',
      'armorMaterialType',
      'weight',
      'mountArmor',
      'upkeep',
      'price',
    ],
    [ItemType.OneHandedWeapon]: [
      'weaponUsage',
      'flags',
      'weight',
      'length',
      'handling',
      'thrustDamage',
      'thrustSpeed',
      'swingDamage',
      'swingSpeed',
      'upkeep',
      'price',
    ],
    [ItemType.Polearm]: [
      'weaponUsage',
      'flags',
      'weight',
      'length',
      'handling',
      'thrustDamage',
      'thrustSpeed',
      'swingDamage',
      'swingSpeed',
      'upkeep',
      'price',
    ],
    [ItemType.Shield]: [
      'flags',
      'weight',
      'length',
      'shieldSpeed',
      'shieldDurability',
      'shieldArmor',
      'upkeep',
      'price',
    ],
    [ItemType.ShoulderArmor]: [
      'culture',
      'flags',
      'armorMaterialType',
      'weight',
      'headArmor',
      'bodyArmor',
      'armArmor',
      'upkeep',
      'price',
    ],
    [ItemType.Thrown]: [
      'damage',
      'missileSpeed',
      'stackWeight',
      'stackAmount',
      'upkeep',
      'price',
    ],
    [ItemType.TwoHandedWeapon]: [
      'flags',
      'weight',
      'length',
      'handling',
      'thrustDamage',
      'thrustSpeed',
      'swingDamage',
      'swingSpeed',
      'upkeep',
      'price',
    ],
    // banners are all the same, no need for aggregation
    [ItemType.Banner]: [
      'flags',
      'weight',
      'culture',
      'upkeep',
      'price',
    ],
  }

export const aggregationsKeysByWeaponClass: Partial<Record<WeaponClass, Array<keyof ItemFlat>>>
  = {
    [WeaponClass.Arrow]: [
      'damageType',
      'damage',
      'stackWeight',
      'stackAmount',
      'upkeep',
      'price',
    ],
    [WeaponClass.Bolt]: [
      'damageType',
      'damage',
      'stackWeight',
      'stackAmount',
      'upkeep',
      'price',
    ],
    [WeaponClass.Cartridge]: [
      'damageType',
      'damage',
      'weight',
      'stackAmount',
      'upkeep',
      'price',
    ],
    [WeaponClass.Bow]: [
      'flags',
      'weight',
      'damage',
      'accuracy',
      'missileSpeed',
      'reloadSpeed',
      'aimSpeed',
      'upkeep',
      'price',
    ],
    [WeaponClass.Crossbow]: [
      'flags',
      'weight',
      'damage',
      'accuracy',
      'missileSpeed',
      'reloadSpeed',
      'aimSpeed',
      'requirement',
      'upkeep',
      'price',
    ],
    [WeaponClass.Musket]: [
      'flags',
      'weight',
      'damage',
      'accuracy',
      'missileSpeed',
      'reloadSpeed',
      'aimSpeed',
      'requirement',
      'upkeep',
      'price',
    ],
    [WeaponClass.Pistol]: [
      'flags',
      'weight',
      'damage',
      'accuracy',
      'missileSpeed',
      'reloadSpeed',
      'aimSpeed',
      'requirement',
      'upkeep',
      'price',
    ],
    [WeaponClass.Dagger]: [
      'length',
      'weight',
      'handling',
      'thrustDamage',
      'thrustSpeed',
      'swingDamage',
      'swingSpeed',
      'upkeep',
      'price',
    ],
    [WeaponClass.Javelin]: [
      'flags',
      'damage',
      'missileSpeed',
      'stackWeight',
      'stackAmount',
      'upkeep',
      'price',
    ],
    [WeaponClass.Mace]: [
      'flags',
      'weight',
      'length',
      'handling',
      'thrustDamage',
      'thrustSpeed',
      'swingDamage',
      'swingSpeed',
      'upkeep',
      'price',
    ],
    [WeaponClass.OneHandedAxe]: [
      'flags',
      'weight',
      'weaponUsage',
      'length',
      'handling',
      'swingDamage',
      'swingSpeed',
      'upkeep',
      'price',
    ],
    [WeaponClass.OneHandedSword]: [
      'weaponUsage',
      'weight',
      'length',
      'handling',
      'thrustDamage',
      'thrustSpeed',
      'swingDamage',
      'swingSpeed',
      'upkeep',
      'price',
    ],
    [WeaponClass.ThrowingAxe]: [
      'flags',
      'damage',
      'missileSpeed',
      'stackWeight',
      'stackAmount',
      'upkeep',
      'price',
    ],
    [WeaponClass.ThrowingKnife]: [
      'damage',
      'weaponUsage',
      'missileSpeed',
      'stackWeight',
      'stackAmount',
      'upkeep',
      'price',
    ],
    [WeaponClass.TwoHandedAxe]: [
      'flags',
      'weight',
      'length',
      'handling',
      'swingDamage',
      'swingSpeed',
      'upkeep',
      'price',
    ],
    [WeaponClass.TwoHandedMace]: [
      'flags',
      'weight',
      'length',
      'handling',
      'swingDamage',
      'swingSpeed',
      'upkeep',
      'price',
    ],
  }
