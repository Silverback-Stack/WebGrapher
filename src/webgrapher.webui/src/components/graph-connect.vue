<template>
  <nav class="card">
    <header class="card-header">
      <p class="card-header-title is-size-4">Connect to a Graph</p>

      <!--New WebGraph-->
      <div class="card-header-icon">
        <b-button type="is-primary" outlined @click="toggleGraphCreate">
          <span class="icon">
            <i class="mdi mdi-plus-thick"></i>
          </span>
          <span>New</span>
        </b-button>
      </div>
    </header>

    <div class="card-content scrollable-content">
      <div class="graph-connect">
        <div v-if="graphs.length === 0" class="has-text-grey">
          No graphs found. Try creating one.
        </div>
        <div v-else>
          <ul>
            <li v-for="graph in graphs" :key="graph.id">
              <a class="panel-block is-clickable subtle-hover mb-2 is-rounded" @click="selectGraph(graph)">
                <div class="is-fullwidth">
                  <strong class="has-text-primary">{{ graph.name }}</strong>
                  <p>{{ graph.description }}</p>
                  <p>{{ graph.url }}</p>
                </div>
              </a>
            </li>
          </ul>
        </div>
      </div>
    </div>

    <footer class="card-footer p-3 is-justify-content-center">
      <!-- Pager -->
      <b-pagination v-if="graphs.length > 0 && totalCount > 0"
                    v-model="localPage"
                    :total="totalCount"
                    :per-page="pageSize"
                    rounded
                    size="is-small" />
    </footer>
  </nav>
</template>

<script setup>
  import { ref, defineProps, defineEmits, watch } from 'vue'
  import { useRouter } from "vue-router"

  const props = defineProps({
    graphs: { type: Array, default: () => [] },
    page: { type: Number, default: 1 },
    pageSize: { type: Number, default: 10 },
    totalCount: { type: Number, default: 0 }
  })

  const emit = defineEmits(["selectGraph", "createGraph", 'changePage'])

  const localPage = ref(props.page)


  // Keep localPage in sync with parent for pager
  watch(() => props.page, val => {
    localPage.value = val
  })

  // Emit page changes
  watch(localPage, newPage => {
    emit('changePage', newPage, props.pageSize)
  })

  function selectGraph(graph) {
    emit("selectGraph", graph)
  }

  function toggleGraphCreate() {
    emit('createGraph')
  }
</script>

<style scoped>
  .subtle-hover:hover {
    background-color: rgba(0,0,0,0.05); /* very light gray */
  }
</style>
