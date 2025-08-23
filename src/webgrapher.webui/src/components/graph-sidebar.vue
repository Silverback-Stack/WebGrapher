<template>
  <b-sidebar type="is-light"
             :model-value="open"
             @update:open="handleClose"
             :fullheight="fullheight"
             :right="right"
             :can-cancel="false">
    <header class="sidebar-header">
      <h3 v-if="node">{{ node.label }}</h3>
      <b-button type="is-light"
                icon-left="close"
                class="is-small"
                @click="handleClose">
        Close
      </b-button>
    </header>

    <hr />

    <div v-if="node">
      <img :src="node.image" alt="Preview" />
      <h3>{{ node.label }}</h3>
      <p>{{ node.summary }}</p>
      <p><strong>Tags:</strong> {{ node.tags?.join(', ') }}</p>
      <p v-if="node.url"><strong>URL:</strong> <a :href="node.url" target="_blank">{{ node.url }}</a></p>

      <ul>
        <li v-for="link in node.outgoingLinks" :key="link">{{ link }}</li>
      </ul>

      <b-button @click="$emit('crawl', node)">Crawl</b-button>
      <b-button @click="$emit('focus', node)">Focus</b-button>
    </div>
  </b-sidebar>
</template>

<script setup>
  import { defineProps, defineEmits } from 'vue'

  const emit = defineEmits(["update:model-value", "crawl", "focus"])

  function handleClose() {
    emit("update:model-value", false)
  }

  const { open, node, fullheight, right } = defineProps({
    open: Boolean,
    node: Object,
    fullheight: { type: Boolean, default: true },
    right: { type: Boolean, default: true }
  })

</script>

<style scoped>
  .sidebar-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.5rem;
  }

  .sidebar-content {
    padding: 1rem;
  }
</style>
