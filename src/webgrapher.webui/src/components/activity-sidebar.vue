<template>
  <aside class="activity-sidebar" v-if="modelValue">
    <!-- Close -->
    <b-button class="activity-sidebar-close"
              type="is-light"
              size="is-small"
              icon-left="close"
              @click="handleActivitySidebarClose" />

    <div class="activity-sidebar-content">

      <div class="activity-header">
        <h1 class="activity-sidebar-title title is-3">Activity</h1>

        <p class="is-flex is-justify-content-space-between">
          <span>Log Stream</span>
          <a class="has-text-link is-clickable is-size-6"
             href="#"
             @click="$emit('clear-activity')">Clear</a>
        </p>
      </div>

      <div class="activity-scroll">
        <ul class="log-list">
          <li v-for="log in activityLogs"
              :key="log.id"
              class="log-entry mb-1">

            <span class="log-message">
              <a class="has-text-link is-clickable is-size-6"
                 @click="toggleExpand(log.id)">

                <i v-if="log.service === 'CRAWLER'"
                   :class="[
                     'mdi mdi-spider icon',
                     log.type === 'Error' ? 'has-text-danger' : '',
                     log.type === 'Warning' ? 'has-text-warning' : ''
                   ]">
                </i>

                <i v-if="log.service === 'SCRAPER'"
                   :class="[
                     'mdi mdi-cloud-download icon',
                     log.type === 'Error' ? 'has-text-danger' : '',
                     log.type === 'Warning' ? 'has-text-warning' : ''
                   ]">
                </i>

                <i v-if="log.service === 'NORMALISATION'"
                   :class="[
                     'mdi mdi-text-box icon',
                     log.type === 'Error' ? 'has-text-danger' : '',
                     log.type === 'Warning' ? 'has-text-warning' : ''
                   ]">
                </i>

                <i v-if="log.service === 'GRAPHING'"
                   :class="[
                     'mdi mdi-graph icon',
                     log.type === 'Error' ? 'has-text-danger' : '',
                     log.type === 'Warning' ? 'has-text-warning' : ''
                   ]">
                </i>

                <i v-if="log.service === 'STREAMING'"
                   :class="[
                     'mdi mdi-broadcast icon',
                     log.type === 'Error' ? 'has-text-danger' : '',
                     log.type === 'Warning' ? 'has-text-warning' : ''
                   ]">
                </i>

                {{ log.message }}
              </a>
            </span>

            <pre v-if="expanded === log.id" class="log-context">{{ JSON.stringify(log, null, 2) }}</pre>

            <hr class="log-divider">
          </li>
        </ul>
      </div>

    </div>
  </aside>
</template>

<script setup>
  import { defineProps, defineEmits, ref } from 'vue'

  const emit = defineEmits(["update:modelValue", "clear-activity"])

  const { modelValue, activityLogs } = defineProps({
    modelValue: Boolean,
    activityLogs: Array
  })

  const expanded = ref(null)

  function handleActivitySidebarClose() {
    emit("update:modelValue", false)
  }

  function toggleExpand(id) {
    expanded.value = expanded.value === id ? null : id
  }
</script>

<style scoped>
  .activity-sidebar {
    position: fixed;
    top: 60px; /* offset for navbar */
    right: 0; /* appear on the right side */
    width: 320px;
    height: calc(100vh - 60px); /* full height minus navbar */
    background-color: rgba(236, 240, 241, .9);
    border-left: 1px solid #ddd;
    box-shadow: -2px 0 6px rgba(0,0,0,0.2);
    z-index: 1000; /* above graph but under modal */
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }

  .activity-sidebar-close {
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
  }

  .activity-sidebar-title {
    padding-right: 2.5rem; /* leaves space for close button */
    margin-top: 0.4rem !important;
    margin-bottom: 1rem !important;
  }

  .activity-sidebar-content {
    flex: 1 1 auto;
    display: flex;
    flex-direction: column;
    padding: 1rem;
    min-height: 0;
  }

  .activity-header {
    /* Non-scrolling */
    flex: 0 0 auto;
  }

  .activity-scroll {
    /* Scrolling column */
    flex: 1 1 auto;
    display: flex;
    flex-direction: column;
    min-height: 0;
    overflow-y: auto;
    border-top: 1px solid #ccc;
    padding-top: 0.5rem;
    overflow-y: scroll; /* keep scrolling enabled */
    scrollbar-width: none; /* Firefox */
    -ms-overflow-style: none; /* IE and Edge */
  }

  .activity-scroll::-webkit-scrollbar {
    display: none; /* Chrome, Safari, Opera */
  }

  .log-list {
    margin: 0;
    padding-left: 0;
  }

  .log-message a {
    display: inline-block; /* ensures alignment with surrounding text */
    margin: 0;
    padding: 0;
  }

  .log-divider {
    border: none;
    border-top: 1px solid #ddd;
    margin: 0.25rem 0;
  }

  .log-context {
    font-family: monospace;
    line-height: 1.3;
    margin: 0.25rem 0;
  }

</style>
