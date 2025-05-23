import { pick } from 'es-toolkit'

import type { Item, ItemFlat, WeaponClass } from '~/models/item'
import type { FiltersModel } from '~/models/item-search'

import { ItemType } from '~/models/item'
import {
  filterItemsByType,
  filterItemsByWeaponClass,
  generateEmptyFiltersModel,
  getAggregationBy,
  getAggregationsConfig,
  getScopeAggregations,
  getVisibleAggregationsConfig,
} from '~/services/item-search-service'
import { createItemIndex } from '~/services/item-search-service/indexator'
import { getWeaponClassesByItemType } from '~/services/item-service'

export const useItemsFilter = (items: Item[]) => {
  const route = useRoute()
  const router = useRouter()

  const itemTypeModel = computed({
    get() {
      return (route.query?.type as ItemType) || ItemType.OneHandedWeapon
    },

    set(val: ItemType) {
      const weaponClasses = getWeaponClassesByItemType(val)

      router.push({
        query: {
          type: val,
          ...(weaponClasses.length !== 0 && { weaponClass: weaponClasses[0] }),
          ...pick(route.query, ['hideOwnedItems']),
        },
      })
    },
  })

  const weaponClassModel = computed({
    get() {
      if (route.query?.weaponClass) { return route.query.weaponClass as WeaponClass }

      const weaponClasses = getWeaponClassesByItemType(itemTypeModel.value)
      return weaponClasses.length !== 0 ? weaponClasses[0] : null
    },

    set(val: WeaponClass | null) {
      router.push({
        query: {
          type: itemTypeModel.value,
          weaponClass: val === null ? undefined : val,
          ...pick(route.query, ['hideOwnedItems']),
        },
      })
    },
  })

  const filterModel = computed({
    get() {
      return {
        ...generateEmptyFiltersModel(aggregationsConfig.value),
        // @ts-expect-error TODO:
        ...('filter' in route.query && (route.query.filter as FiltersModel)),
      }
    },

    set(val: FiltersModel<string[] | number[]>) {
      router.push({
        query: {
          ...route.query,
          // @ts-expect-error TODO:
          filter: val,
        },
      })
    },
  })

  const updateFilter = (key: keyof ItemFlat, val: string | string[] | number | number[]) => {
    router.push({
      query: {
        ...route.query,
        filter: { ...filterModel.value, [key]: val },
      },
    })
  }

  const hideOwnedItemsModel = computed({
    get() {
      return Boolean(route.query.hideOwnedItems) || false
    },

    set(val: boolean) {
      router.push({
        query: {
          ...route.query,
          hideOwnedItems: val === false ? undefined : String(val),
        },
      })
    },
  })

  const resetFilters = () => {
    router.push({
      query: {
        ...pick(route.query, [
          'type',
          'weaponClass',
          'sort',
          'perPage',

          // TODO: need to be purged?
          'isCompareActive',
          'compareList',

          'hideOwnedItems',
        ]), // TODO: keys to config?
      },
    })
  }

  const flatItems = computed((): ItemFlat[] => createItemIndex(items, true))

  const aggregationsConfig = computed(() =>
    getAggregationsConfig(itemTypeModel.value, weaponClassModel.value),
  )

  const aggregationsConfigVisible = computed(() =>
    getVisibleAggregationsConfig(aggregationsConfig.value),
  )

  const filteredByTypeFlatItems = computed((): ItemFlat[] =>
    filterItemsByType(flatItems.value, itemTypeModel.value),
  )

  const filteredByClassFlatItems = computed((): ItemFlat[] =>
    filterItemsByWeaponClass(filteredByTypeFlatItems.value, weaponClassModel.value),
  )

  const aggregationByType = computed(() => getAggregationBy(flatItems.value, 'type'))
  const aggregationByClass = computed(() =>
    getAggregationBy(filteredByTypeFlatItems.value, 'weaponClass'),
  )

  // needed for the range slider to work normal.
  const scopeAggregations = computed(() =>
    getScopeAggregations(filteredByClassFlatItems.value, aggregationsConfig.value),
  )

  return {
    aggregationByClass,
    aggregationByType,
    aggregationsConfig,
    aggregationsConfigVisible,

    filteredByClassFlatItems,

    filteredByTypeFlatItems,

    filterModel,
    hideOwnedItemsModel,

    itemTypeModel,
    resetFilters,

    scopeAggregations,
    updateFilter,
    weaponClassModel,
  }
}
