<template>
  <v-navigation-drawer
    app
    :model-value="drawer"
    @update:model-value="emit('update:drawer', $event)"
    width="260"
  >
    <v-list nav dense>

      <template v-for="item in menu" :key="item.id">

        <!-- ITEM SIMPLE -->
        <v-list-item
          v-if="!item.children || item.children.length === 0"
          :to="item.ruta"
          router
          link
        >
          <v-list-item-icon>
            <v-icon>{{ item.icono }}</v-icon>
          </v-list-item-icon>
          <v-list-item-title>{{ item.titulo }}</v-list-item-title>
        </v-list-item>

        <!-- ITEM PADRE -->
        <v-list-group
          v-else
          :value="isGroupOpen(item)"
        >
          <template #activator="{ props }">
            <v-list-item v-bind="props">
              <v-list-item-icon>
                <v-icon>{{ item.icono }}</v-icon>
              </v-list-item-icon>
              <v-list-item-title>{{ item.titulo }}</v-list-item-title>
            </v-list-item>
          </template>

          <v-list-item
            v-for="child in item.children"
            :key="child.id"
            :to="child.ruta"
            router
            link
          >
            <v-list-item-icon>
              <v-icon>{{ child.icono }}</v-icon>
            </v-list-item-icon>
            <v-list-item-title>{{ child.titulo }}</v-list-item-title>
          </v-list-item>

        </v-list-group>

      </template>

    </v-list>
  </v-navigation-drawer>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import MenuService from '../../api/menu.service'

/* =========================
   PROPS & EMITS
========================= */
const props = defineProps({
  drawer: {
    type: Boolean,
    required: true
  }
})

const emit = defineEmits(['update:drawer'])

/* =========================
   STATE
========================= */
const menu = ref([])
const route = useRoute()

/* =========================
   METHODS
========================= */
function isGroupOpen(item) {
  return item.children?.some(child => child.ruta === route.path)
}

/* =========================
   LIFECYCLE
========================= */
onMounted(async () => {
  const response = await MenuService.getMenuTree()
  menu.value = response.data
})
</script>
