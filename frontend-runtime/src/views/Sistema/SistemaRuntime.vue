<template>
  <v-container fluid :style="themeStyle" :class="['runtime-container', uiMode]">
    <v-row class="mb-4 align-center sb-page-header">
      <v-col>
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="26">mdi-database</v-icon>
          </div>
          <div>
            <h2 class="mb-1">{{ systemTitle }}</h2>
            <span class="sb-page-subtitle text-body-2">
              {{ entidadSeleccionada ? `/${entidadRoute(entidadSeleccionada)}` : '/' }}
            </span>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn color="primary" :disabled="!entidadSeleccionada" @click="nuevoRegistro">
          <v-icon left>mdi-plus</v-icon>
          Nuevo registro
        </v-btn>
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="12" md="3">
        <v-card elevation="2" class="card">
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-folder-outline</v-icon>
            <span class="text-h6">Vistas</span>
          </v-card-title>
          <v-divider />
          <v-list :density="uiDensity">
            <v-list-item
              v-for="entidad in runtimeEntities"
              :key="entidad.entityId || entidad.id || entidad.name"
              :active="entidadSeleccionada?.entityId === entidad.entityId"
              :prepend-icon="entidadMenuIcon(entidad)"
              @click="irEntidad(entidad)"
            >
              <v-list-item-title>{{ entidadLabel(entidad) }}</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-card>
      </v-col>

      <v-col cols="12" md="9">
        <v-card elevation="2" class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-table</v-icon>
              <span class="text-h6">{{ entidadTitulo }}</span>
            </div>
            <v-btn icon variant="text" @click="cargarDatos" :disabled="!entidadSeleccionada">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />

          <v-row v-if="showSearch || showFilters" class="px-4 py-2" dense>
            <v-col v-if="showSearch" cols="12" md="4">
              <v-text-field
                v-model="search"
                label="Buscar"
                clearable
                prepend-inner-icon="mdi-magnify"
                :density="uiDensity"
              />
            </v-col>
            <v-col v-if="showFilters" cols="12" md="4">
              <v-select
                v-model="filterField"
                :items="filterFields"
                item-title="title"
                item-value="value"
                label="Filtrar por"
                clearable
                :density="uiDensity"
              />
            </v-col>
            <v-col v-if="showFilters" cols="12" md="4">
              <v-text-field
                v-model="filterValue"
                label="Valor"
                :disabled="!filterField"
                clearable
                :density="uiDensity"
              />
            </v-col>
          </v-row>

          <v-alert v-if="error" type="error" variant="tonal" class="ma-4">
            {{ error }}
          </v-alert>

          <div v-if="loading" class="pa-4">Cargando...</div>

          <div v-else-if="!entidadSeleccionada" class="pa-4">
            Selecciona una vista para ver registros.
          </div>

          <v-data-table
            v-else
            :headers="headers"
            :items="paginatedRegistros"
            class="table"
            :density="uiDensity"
            :fixed-header="listStickyHeader"
            :height="listStickyHeader ? 420 : undefined"
            :no-data-text="entityMessages.empty"
            hover
          >
            <template #item="{ item, columns }">
              <tr>
                <td v-for="col in columns" :key="col.key">
                  <template v-if="col.key === 'actions'">
                    <v-tooltip text="Editar">
                      <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="primary" variant="text" @click="editarRegistro(item.raw || item)">
                          <v-icon>mdi-pencil</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>
                    <v-tooltip v-if="enableDuplicate" text="Duplicar">
                      <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="blue" variant="text" @click="duplicarRegistro(item.raw || item)">
                          <v-icon>mdi-content-copy</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>
                    <v-tooltip v-if="quickToggleField" :text="`Toggle ${quickToggleField.label || quickToggleField.name}`">
                      <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="teal" variant="text" @click="toggleQuickField(item.raw || item)">
                          <v-icon>mdi-toggle-switch</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>
                    <v-tooltip text="Eliminar">
                      <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="red" variant="text" @click="eliminarRegistro(item.raw || item)">
                          <v-icon>mdi-delete</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>
                  </template>
                  <template v-else>
                    <template v-if="formattedCell(item.raw || item, col).isChip">
                      <v-chip size="small" :color="formattedCell(item.raw || item, col).color">
                        {{ formattedCell(item.raw || item, col).text }}
                      </v-chip>
                    </template>
                    <template v-else>
                      {{ formattedCell(item.raw || item, col).text }}
                    </template>
                  </template>
                </td>
              </tr>
            </template>
          </v-data-table>

          <div v-if="listShowTotals" class="px-4 pt-2 text-caption text-medium-emphasis">
            Total: {{ sortedRegistros.length }} registros
          </div>

          <v-row class="px-4 pb-4 pt-2 align-center" dense>
            <v-col cols="12" md="4">
              <v-select
                v-model="itemsPerPage"
                :items="itemsPerPageOptions"
                label="Filas por pagina"
                :density="uiDensity"
              />
            </v-col>
            <v-col cols="12" md="8" class="d-flex justify-end">
              <v-pagination
                v-model="page"
                :length="pageCount"
                density="compact"
              />
            </v-col>
          </v-row>
        </v-card>
      </v-col>
    </v-row>

    <RegistroDialog
      v-model="dialog"
      :record="registroActual"
      :fields="campos"
      :layout="formLayout"
      :density="uiDensity"
      :messages="entityMessages"
      :confirm-save="confirmSave"
      :mode="dialogMode"
      :api-route="apiRoute"
      @guardado="cargarDatos"
    />
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import frontendConfig from '../../config/frontend-config.json'
import { toKebab } from '../../utils/slug.js'
import RegistroDialog from '../../components/sistemas/RegistroDialog.vue'
import runtimeApi from '../../api/runtime.service.js'

const route = useRoute()
const router = useRouter()

const config = ref(JSON.parse(JSON.stringify(frontendConfig || {})))

const registros = ref([])
const loading = ref(false)
const error = ref('')

const search = ref('')
const filterField = ref(null)
const filterValue = ref('')

const page = ref(1)
const itemsPerPage = ref(10)

const dialog = ref(false)
const dialogMode = ref('create')
const registroActual = ref(null)

const entidadSeleccionada = ref(null)

const systemTitle = computed(() => config.value?.system?.appTitle || 'Sistema')

const uiDensity = computed(() => config.value?.system?.density || 'comfortable')
const uiMode = computed(() => config.value?.system?.uiMode || 'enterprise')
const locale = computed(() => config.value?.system?.locale || 'es-AR')
const currency = computed(() => config.value?.system?.currency || 'ARS')

const themeStyle = computed(() => ({
  '--sb-primary': config.value?.system?.primaryColor || '#2563eb',
  '--sb-secondary': config.value?.system?.secondaryColor || '#0ea5e9',
  '--sb-font': config.value?.system?.fontFamily || 'Inter, system-ui, -apple-system, Segoe UI, sans-serif'
}))

const entities = computed(() => config.value?.entities || [])

const runtimeEntities = computed(() => entities.value.filter(entity => entity.showInMenu !== false))

function entidadRoute(entidad) {
  return toKebab(entidad?.routeSlug || entidad?.name || entidad?.menuLabel || 'item')
}

function entidadLabel(entidad) {
  return entidad?.menuLabel || entidad?.displayName || entidad?.name || 'Entidad'
}

function entidadMenuIcon(entidad) {
  return entidad?.menuIcon || 'mdi-table'
}

const entitySlug = computed(() => route.params.entity || '')

const entidadTitulo = computed(() => entidadSeleccionada.value ? entidadLabel(entidadSeleccionada.value) : 'Entidad')

const campos = computed(() => entidadSeleccionada.value?.fields || [])

const listFields = computed(() => campos.value.filter(field => field.showInList !== false))

const pkField = computed(() => {
  return campos.value.find(f => f.isPrimaryKey) || campos.value.find(f => String(f.columnName || f.name).toLowerCase() === 'id')
})

const quickToggleField = computed(() => campos.value.find(f => f.quickToggle))

const entityMessages = computed(() => entidadSeleccionada.value?.messages || {
  empty: 'No hay registros todavia.',
  error: 'Ocurrio un error al procesar la solicitud.',
  successCreate: 'Registro creado.',
  successUpdate: 'Registro actualizado.',
  successDelete: 'Registro eliminado.'
})

const listStickyHeader = computed(() => entidadSeleccionada.value?.listStickyHeader === true)
const listShowTotals = computed(() => entidadSeleccionada.value?.listShowTotals !== false)
const formLayout = computed(() => entidadSeleccionada.value?.formLayout || 'single')
const confirmSave = computed(() => entidadSeleccionada.value?.confirmSave !== false)
const confirmDelete = computed(() => entidadSeleccionada.value?.confirmDelete !== false)
const enableDuplicate = computed(() => entidadSeleccionada.value?.enableDuplicate !== false)

const apiRoute = computed(() => (entidadSeleccionada.value ? entidadRoute(entidadSeleccionada.value) : ''))

const itemsPerPageOptions = computed(() => config.value?.system?.itemsPerPageOptions || [10, 20, 50])

const showSearch = computed(() => config.value?.system?.showSearch !== false)
const showFilters = computed(() => config.value?.system?.showFilters !== false)

const filterFields = computed(() => listFields.value.filter(f => f.showInFilter !== false).map(f => ({
  title: f.label || f.name || f.columnName,
  value: f.columnName
})))

const filteredRegistros = computed(() => {
  let items = [...registros.value]

  if (search.value) {
    const term = search.value.toLowerCase()
    items = items.filter(item => {
      return listFields.value.some(field => {
        const value = item[field.columnName]
        return value != null && value.toString().toLowerCase().includes(term)
      })
    })
  }

  if (filterField.value && filterValue.value) {
    const term = filterValue.value.toLowerCase()
    items = items.filter(item => {
      const value = item[filterField.value]
      return value != null && value.toString().toLowerCase().includes(term)
    })
  }

  return items
})

const sortedRegistros = computed(() => {
  const items = [...filteredRegistros.value]
  const entity = entidadSeleccionada.value
  if (!entity) return items

  const sortFieldId = entity.defaultSortFieldId
  const sortField = campos.value.find(f => f.fieldId === sortFieldId) || pkField.value
  const sortKey = sortField?.columnName
  const dir = entity.defaultSortDirection === 'desc' ? -1 : 1

  if (!sortKey) return items

  items.sort((a, b) => {
    const va = a[sortKey]
    const vb = b[sortKey]
    if (va == null && vb == null) return 0
    if (va == null) return -1 * dir
    if (vb == null) return 1 * dir
    if (typeof va === 'number' && typeof vb === 'number') return (va - vb) * dir
    const sa = va.toString().toLowerCase()
    const sb = vb.toString().toLowerCase()
    if (sa < sb) return -1 * dir
    if (sa > sb) return 1 * dir
    return 0
  })

  return items
})

const pageCount = computed(() => {
  const total = sortedRegistros.value.length
  return total === 0 ? 1 : Math.ceil(total / itemsPerPage.value)
})

const paginatedRegistros = computed(() => {
  const start = (page.value - 1) * itemsPerPage.value
  const end = start + itemsPerPage.value
  return sortedRegistros.value.slice(start, end)
})

const headers = computed(() => {
  const cols = listFields.value.map(field => ({
    title: field.label || field.name || field.columnName,
    key: field.columnName
  }))

  return [
    ...cols,
    { title: 'Acciones', key: 'actions', sortable: false }
  ]
})

function normalizeConfig() {
  if (!config.value?.system) config.value.system = {}
  const sys = config.value.system
  sys.primaryColor = sys.primaryColor || '#2563eb'
  sys.secondaryColor = sys.secondaryColor || '#0ea5e9'
  sys.density = sys.density || 'comfortable'
  sys.fontFamily = sys.fontFamily || 'Inter, system-ui, -apple-system, Segoe UI, sans-serif'
  sys.uiMode = sys.uiMode || 'enterprise'
  sys.locale = sys.locale || 'es-AR'
  sys.currency = sys.currency || 'ARS'

  if (!Array.isArray(config.value.entities)) config.value.entities = []

  config.value.entities.forEach(entity => {
    if (entity.showInMenu === undefined) entity.showInMenu = true
    if (!entity.menuIcon) entity.menuIcon = 'mdi-table'
    if (entity.routeSlug === undefined) entity.routeSlug = ''
    if (entity.listStickyHeader === undefined) entity.listStickyHeader = false
    if (entity.listShowTotals === undefined) entity.listShowTotals = true
    if (!entity.defaultSortDirection) entity.defaultSortDirection = 'asc'
    if (!entity.formLayout) entity.formLayout = 'single'
    if (entity.confirmSave === undefined) entity.confirmSave = true
    if (entity.confirmDelete === undefined) entity.confirmDelete = true
    if (entity.enableDuplicate === undefined) entity.enableDuplicate = true
    if (!entity.messages) {
      entity.messages = {
        empty: 'No hay registros todavia.',
        error: 'Ocurrio un error al procesar la solicitud.',
        successCreate: 'Registro creado.',
        successUpdate: 'Registro actualizado.',
        successDelete: 'Registro eliminado.'
      }
    }
    if (!Array.isArray(entity.fields)) entity.fields = []
    entity.fields.forEach(field => {
      if (field.placeholder === undefined) field.placeholder = ''
      if (field.helpText === undefined) field.helpText = ''
      if (field.inputType === undefined) field.inputType = ''
      if (field.section === undefined) field.section = 'General'
      if (field.format === undefined) field.format = ''
      if (field.min === undefined) field.min = null
      if (field.max === undefined) field.max = null
      if (field.pattern === undefined) field.pattern = ''
      if (field.quickToggle === undefined) field.quickToggle = false
    })
  })
}

function resolverEntidad() {
  if (!runtimeEntities.value.length) {
    entidadSeleccionada.value = null
    return
  }

  const target = entitySlug.value
    ? runtimeEntities.value.find(ent => entidadRoute(ent) === entitySlug.value)
    : runtimeEntities.value[0]

  if (!target) {
    router.replace(`/${entidadRoute(runtimeEntities.value[0])}`)
    return
  }

  entidadSeleccionada.value = target
  cargarDatos()
}

function irEntidad(entidad) {
  const slug = entidadRoute(entidad)
  router.push(`/${slug}`)
}

async function cargarDatos() {
  if (!entidadSeleccionada.value) return
  loading.value = true
  error.value = ''
  try {
    const { data } = await runtimeApi.list(apiRoute.value)
    registros.value = Array.isArray(data) ? data : (data?.items || [])
  } catch (err) {
    error.value = entityMessages.value.error
  } finally {
    loading.value = false
  }
}

function nuevoRegistro() {
  dialogMode.value = 'create'
  registroActual.value = null
  dialog.value = true
}

function editarRegistro(item) {
  dialogMode.value = 'edit'
  registroActual.value = { ...item }
  dialog.value = true
}

function duplicarRegistro(item) {
  dialogMode.value = 'duplicate'
  registroActual.value = { ...item }
  dialog.value = true
}

async function eliminarRegistro(item) {
  if (!pkField.value) return
  if (confirmDelete.value) {
    const ok = window.confirm(entityMessages.value.confirmDelete || 'Eliminar registro?')
    if (!ok) return
  }

  try {
    await runtimeApi.remove(apiRoute.value, item[pkField.value.columnName])
    await cargarDatos()
  } catch (err) {
    window.alert(entityMessages.value.error)
  }
}

async function toggleQuickField(item) {
  if (!quickToggleField.value) return
  if (!pkField.value) return

  const payload = { ...item }
  const key = quickToggleField.value.columnName
  payload[key] = !payload[key]

  try {
    await runtimeApi.update(apiRoute.value, item[pkField.value.columnName], payload)
    await cargarDatos()
  } catch (err) {
    window.alert(entityMessages.value.error)
  }
}

function formattedCell(item, col) {
  const field = campos.value.find(f => f.columnName === col.key)
  if (!field) return { text: item[col.key], isChip: false }

  let value = item[col.key]
  const format = field.format
  const dataType = String(field.dataType || '').toLowerCase()

  if (value == null) return { text: '', isChip: false }

  if (format === 'uppercase') {
    value = String(value).toUpperCase()
  }

  if (format === 'money') {
    const formatter = new Intl.NumberFormat(locale.value, {
      style: 'currency',
      currency: currency.value
    })
    return { text: formatter.format(value), isChip: false }
  }

  if (format === 'date' || dataType.includes('date')) {
    const date = new Date(value)
    if (!Number.isNaN(date.getTime())) {
      return { text: date.toLocaleDateString(locale.value), isChip: false }
    }
  }

  if (format === 'datetime') {
    const date = new Date(value)
    if (!Number.isNaN(date.getTime())) {
      return { text: date.toLocaleString(locale.value), isChip: false }
    }
  }

  if (format === 'badge') {
    return { text: value, isChip: true, color: value ? 'green' : 'red' }
  }

  if (dataType.includes('bit') || dataType.includes('bool')) {
    return { text: value ? 'Si' : 'No', isChip: true, color: value ? 'green' : 'grey' }
  }

  return { text: value, isChip: false }
}

watch(
  () => entitySlug.value,
  () => resolverEntidad()
)

watch(itemsPerPage, () => {
  page.value = 1
})

onMounted(() => {
  normalizeConfig()
  if (config.value?.system?.defaultItemsPerPage) {
    itemsPerPage.value = config.value.system.defaultItemsPerPage
  }
  resolverEntidad()
})
</script>

<style scoped>
.runtime-container {
  font-family: var(--sb-font, Inter, system-ui, sans-serif);
}

.sb-page-header {
  padding: 12px;
  background: #fff;
  border-radius: 16px;
  box-shadow: 0 6px 14px rgba(15, 23, 42, 0.08);
}

.sb-page-icon {
  width: 48px;
  height: 48px;
  background: rgba(37, 99, 235, 0.12);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 12px;
}

.card {
  border-radius: 16px;
}

.table :deep(th) {
  font-weight: 600;
}
</style>
