<template>
  <v-dialog v-model="model" max-width="400">
    <v-card>
      <v-card-title>
        {{ rol ? 'Editar rol' : 'Nuevo rol' }}
      </v-card-title>

      <v-card-text>
        <v-text-field v-model="form.nombre" label="Nombre" />
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn text @click="cerrar">Cancelar</v-btn>
        <v-btn color="primary" @click="guardar">
          Guardar
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import rolService from '../../api/rol.service.js';

export default {
  props: {
    modelValue: Boolean,
    rol: Object
  },

  emits: ['update:modelValue', 'guardado'],

  data() {
    return {
      form: {
        nombre: ''
      }
    };
  },

  computed: {
    model: {
      get() {
        return this.modelValue;
      },
      set(v) {
        this.$emit('update:modelValue', v);
      }
    }
  },

  watch: {
    rol: {
      immediate: true,
      handler(rol) {
        this.form = {
          nombre: rol ? rol.nombre : ''
        };
      }
    }
  },

  methods: {
    cerrar() {
      this.model = false;
    },

    async guardar() {
      if (this.rol?.id) {
        // EDITAR (solo nombre)
        await rolService.editar(this.rol.id, {
          nombre: this.form.nombre
        });
      } else {
        // CREAR (activo por default en backend)
        await rolService.crear({
          nombre: this.form.nombre
        });
      }

      this.$emit('guardado');
      this.cerrar();
    }
  }
};
</script>
