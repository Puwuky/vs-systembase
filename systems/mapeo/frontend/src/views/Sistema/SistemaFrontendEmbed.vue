<template>
  <v-container fluid class="runtime-container">
    <v-row class="mb-4 align-center sb-page-header">
      <v-col>
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="26">mdi-monitor</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Frontend embebido</h2>
            <span class="sb-page-subtitle text-body-2">
              Esta vista no está habilitada en Mapeo.
            </span>
          </div>
        </div>
      </v-col>
      <v-col cols="auto">
        <v-btn variant="tonal" color="primary" @click="volver">
          <v-icon left>mdi-arrow-left</v-icon>
          Volver
        </v-btn>
      </v-col>
    </v-row>

    <v-alert type="info" variant="tonal">
      El sistema de mapeo usa su propio runtime. Si necesitás embebido, lo podemos activar.
    </v-alert>
  </v-container>
</template>

<script setup>
import { useRouter } from 'vue-router'

const router = useRouter()

function volver() {
  router.back()
}
</script>
