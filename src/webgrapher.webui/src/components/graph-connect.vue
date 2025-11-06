<template>
  <div class="card">
    <header class="card-header">
      <p class="card-header-title is-size-4">Connect to Graph</p>

      <!--New WebGraph-->
      <div class="card-header-icon">
        <b-button type="is-primary" outlined @click="toggleGraphCreate">
          <span class="icon">
            <i class="mdi mdi-plus-thick"></i>
          </span>
          <span>New Graph</span>
        </b-button>
      </div>
    </header>

    <div class="card-content scrollable-content">
      <div class="graph-connect">

        <!-- No Graphs Found -->
        <div v-if="graphs.length === 0" class="is-size-5">
          <p class="mb-2">Create a new graph to get started.</p>
        </div>
        <div v-else>

          <!-- Graph List -->
          <div class="list">
            <div v-for="graph in graphs" :key="graph.id"
                 class="list-item is-flex is-align-items-center is-justify-content-space-between">

              <!-- Row Data -->
              <div class="list-item-content">
                <div class="list-item-title is-size-5">
                  <span v-if="graph.id === props.connectedGraphId"
                        class="icon has-text-success is-size-5 mr-1">
                    <i class="mdi mdi-broadcast"></i>
                  </span>
                  <a @click="connectGraph(graph)">{{ graph.name }}</a>
                </div>
                <div class="list-item-description">{{ graph.description }}</div>
                <div class="list-item-description has-text-grey-light">{{ graph.id }}</div>
              </div>

              <!--Row Action Buttons-->
              <div class="list-item-controls is-right">
                <div class="buttons">

                  <!-- Disconnect Button -->
                  <b-button type="is-primary"
                            v-if="graph.id === props.connectedGraphId"
                            outlined
                            @click.stop="disconnectGraph(graph)">
                    <span class="icon">
                      <i class="mdi mdi-broadcast-off"></i>
                    </span>
                    <span>Disconnect</span>
                  </b-button>

                  <!-- Connect Button -->
                  <b-button type="is-primary"
                            v-else
                            outlined
                            @click.stop="connectGraph(graph)">
                    <span class="icon">
                      <i class="mdi mdi-broadcast"></i>
                    </span>
                    <span>Connect</span>
                  </b-button>

                  <!-- Delete Button -->
                  <b-tooltip type="is-light" position="is-left"
                             :triggers="['click']"
                             :auto-close="['outside', 'escape']">
                    <template v-slot:content>
                      <b-button type="is-danger" outlined
                                :loading="deletingGraphs[graph.id]"
                                :disabled="deletingGraphs[graph.id]"
                                @click="confirmDeleteGraph(graph)">
                        <span class="icon" v-if="!deletingGraphs[graph.id]"><i class="mdi mdi-alert"></i></span>
                        <span class="icon" v-else><i class="mdi mdi-loading mdi-spin"></i></span>
                        <span>{{ deletingGraphs[graph.id] ? 'Deleting...' : 'Confirm Delete' }}</span>
                      </b-button>
                    </template>
                    <b-button type="is-primary" outlined
                              :disabled="deletingGraphs[graph.id]">
                      <span class="icon"><i class="mdi mdi-trash-can-outline"></i></span>
                      <span>Delete</span>
                    </b-button>
                  </b-tooltip>

                </div>
              </div>

            </div>
          </div>

        </div>
      </div>
    </div>

    <!-- Pager -->
    <footer class="card-footer p-3 is-justify-content-center">
      <b-pagination v-if="graphs.length > 0 && totalCount > 0"
                    v-model="localPage"
                    :total="totalCount"
                    :per-page="pageSize"
                    size="is-small" />
    </footer>
  </div>
</template>

<script setup>
  import { ref, reactive, defineProps, defineEmits, watch } from 'vue'
  import { useRouter } from 'vue-router'

  const props = defineProps({
    graphs: { type: Array, default: () => [] },
    page: { type: Number, default: 1 },
    pageSize: { type: Number, default: 8 },
    totalCount: { type: Number, default: 0 },
    connectedGraphId: { type: String, default: null }
  })

  const emit = defineEmits(["connectGraph", "disconnectGraph", "createGraph", 'changePage', 'deleteGraph'])

  const localPage = ref(props.page)

  // Track deleting state per graph id
  const deletingGraphs = reactive({})

  // Keep localPage in sync with parent for pager
  watch(() => props.page, val => {
    localPage.value = val
  })

  // Emit page changes
  watch(localPage, newPage => {
    emit('changePage', newPage, props.pageSize)
  })

  function connectGraph(graph) {
    emit("connectGraph", graph)
  }

  function disconnectGraph(graph) {
    emit("disconnectGraph", graph)
  }

  function toggleGraphCreate() {
    emit('createGraph')
  }

  function confirmDeleteGraph(graph) {
    if (deletingGraphs[graph.id]) return // prevent double click
    deletingGraphs[graph.id] = true
    emit("deleteGraph", graph)
  }
</script>

<style scoped>

  .list {
    background-color: initial;
    box-shadow: none;
    border-radius: 0;
  }

  .list-item {
      padding: 1em 0.5em;
  }

  .list-item a {
    padding-left: 0; 
    margin-left: 0; 
  }

  .list-item-controls {
    opacity: 0;
    transition: opacity 0.2s ease;
  }

  .list-item:hover .list-item-controls {
    opacity: 1;
  }

</style>
