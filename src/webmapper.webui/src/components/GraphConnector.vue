<template>
  <nav class="card">
    <header class="card-header">
      <p class="card-header-title is-size-4">Connect to a WebMap</p>
      <div class="card-header-icon">
        <b-button type="is-primary" outlined @click="toggleGraphCreator">
          <span class="icon">
            <i class="mdi mdi-plus-thick"></i>
          </span>
          <span>New</span>
        </b-button>
      </div>
    </header>

    <div class="card-content">
      <div v-for="graph in graphs" :key="graph.id">
        <a class="panel-block is-clickable subtle-hover mb-2 is-rounded" @click="selectGraph(graph)">
          <div class="is-fullwidth">
            <strong class="has-text-primary">{{ graph.name }}</strong>
            <p>{{ graph.description }}</p>
            <p>{{ graph.url }}</p>
          </div>
        </a>
      </div>
    </div>

    <!--<footer class="card-footer p-3 is-justify-content-center">
    </footer>-->
  </nav>
</template>

<script setup>
  import { ref, defineProps, defineEmits } from 'vue'

  const props = defineProps({
    graphs: { type: Array, required: true }
  })

  const emit = defineEmits(['select'])

  function selectGraph(graph) {
    emit('select', graph)
  }

  const currentPage = ref(1)
  const totalPages = ref(5) // or dynamically calculated

  function onPageChange(page) {
    console.log("New page:", page)
    // fetch or display content for this page
  }
</script>

<style scoped>
  .subtle-hover:hover {
    background-color: rgba(0,0,0,0.05); /* very light gray */
  }
</style>
