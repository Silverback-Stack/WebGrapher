<template>
  <aside class="graph-sidebar" v-if="modelValue">
    <!-- Close -->
    <b-button class="sidebar-close"
              type="is-light"
              size="is-small"
              icon-left="close"
              @click="handleClose" />

    <div class="sidebar-content" v-if="node">

      <!-- Title -->
      <h1 class="sidebar-title title is-3">{{ node.label }}</h1>

      <!-- Image -->
      <a v-if="node?.image"
         :href="node?.id" target="_blank" class="sidebar-image is-block">
        <b-image v-if="node.image"
                 :src="node.image"
                 alt="Preview"
                 responsive />
      </a>

      <!-- Summary -->
      <div>
        <p v-if="node?.summary"
           class="sidebar-summary is-size-5"
           :class="{ 'is-truncated': !isSummaryExpanded }">
          {{ node.summary }}
        </p>

        <a v-if="node?.summary && node?.summary.length > 200"
           class="has-text-link is-clickable is-size-6"
           @click="isSummaryExpanded = !isSummaryExpanded">
          {{ isSummaryExpanded ? 'Show less' : 'Show more' }}
        </a>
      </div>

      <div class="my-4">
        <b-button class="mr-2"
                  type="is-primary"
                  outlined
                  @click="$emit('focus-node', node.id)">Focus</b-button>
        <b-button type="is-primary"
                  outlined
                  @click="$emit('crawl-node', node.id)">Re-crawl</b-button>
      </div>

      <!-- Outgoing Edges -->
      <div v-if="node?.outgoingEdges?.length" class="sidebar-connections my-3">
        <p>Outgoing Links</p>
        <ul>
          <li v-for="(edge, index) in node.outgoingEdges" :key="edge.id" v-show="isOutgoingExpanded || index < 6">
            <a href="#" @click.prevent="$emit('focus-node', edge.id)">{{ edge.title }}</a>
          </li>
        </ul>
        <a v-if="node.outgoingEdges.length > 6"
           class="has-text-link is-clickable is-size-6"
           @click="isOutgoingExpanded = !isOutgoingExpanded">
          {{ isOutgoingExpanded ? 'Show less' : 'Show more' }}
          <span class="icon is-small">
            <i :class="isOutgoingExpanded ? 'mdi mdi-chevron-up' : 'mdi mdi-chevron-down'"></i>
          </span>
        </a>
      </div>

      <!-- Incoming Edges -->
      <div v-if="node?.incomingEdges?.length" class="sidebar-connections my-3">
        <p>Incoming Links</p>
        <ul>
          <li v-for="(edge, index) in node.incomingEdges" :key="edge.id" v-show="isIncomingExpanded || index < 6">
            <a href="#" @click.prevent="$emit('focus-node', edge.id)">{{ edge.title }}</a>
          </li>
        </ul>
        <a v-if="node.incomingEdges.length > 6"
           class="has-text-link is-clickable is-size-6"
           @click="isIncomingExpanded = !isIncomingExpanded">
          {{ isIncomingExpanded ? 'Show less' : 'Show more' }}
          <span class="icon is-small">
            <i :class="isIncomingExpanded ? 'mdi mdi-chevron-up' : 'mdi mdi-chevron-down'"></i>
          </span>
        </a>
      </div>

      <!-- Page Url -->
      <div class="sidebar-source my-3">
        <p>Source</p>
        <a :href="node?.id" target="_blank" class="is-size-6 is-block has-text-break">
          {{ node.id }}
        </a>
      </div>

      <!-- Tags -->
      <div v-if="node?.tags" class="sidebar-tags">
        <b-taglist>
          <b-tag v-for="(tag, index) in node.tags"
                 :key="index"
                 type="is-dark">
            {{ tag }}
          </b-tag>
        </b-taglist>
      </div>

    </div>
  </aside>
</template>

<script setup>
  import { defineProps, defineEmits, ref } from 'vue'
  import { BTag, BTaglist } from "buefy"

  const emit = defineEmits(["update:modelValue", "crawl-node", "focus-node"])

  const isSummaryExpanded = ref(false)
  const isOutgoingExpanded = ref(false)
  const isIncomingExpanded = ref(false)

  const { modelValue, node } = defineProps({
    modelValue: Boolean,
    node: Object
  })

  function handleClose() {
    emit("update:modelValue", false)
  }
</script>

<style scoped>
  .graph-sidebar {
    position: fixed;
    top: 60px; /* offset for navbar */
    left: 0; /* appear on the left side */
    width: 320px;
    height: calc(100vh - 60px); /* full height minus navbar */
    background-color: rgba(236, 240, 241, .9);
    border-right: 1px solid #ddd;
    box-shadow: -2px 0 6px rgba(0,0,0,0.2);
    z-index: 1000; /* above graph but under modal */
    overflow-y: auto;
    /* Blur content behind */
    /*backdrop-filter: blur(8px);
    -webkit-backdrop-filter: blur(8px);*/ /* Safari support */
  }

  .sidebar-close {
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
  }

  .sidebar-title {
    padding-right: 2.5rem; /* leaves space for close button */
    margin-top: 0.4rem !important;
    margin-bottom: 1rem !important;
  }

  .sidebar-image span {
    word-break: break-all;
  }

  .sidebar-content {
    padding: 1rem;
  }

  .sidebar-summary {
    margin-top: 0.4rem !important;
  }

  .is-truncated {
    display: -webkit-box;
    -webkit-line-clamp: 6; /* show only 6 lines */
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .sidebar-tags {
    margin-top: 1rem;
    margin-bottom: 1rem;
  }

  .sidebar-tags .tags .tag {
    margin-right: 0;
    margin-bottom: 0;
  }

  .sidebar-connections a {
    display: block; /* allow width to apply */
    max-width: 100%; /* don’t exceed container */
    white-space: nowrap; /* prevent wrapping */
    overflow: hidden; /* hide overflow */
    text-overflow: ellipsis; /* show "..." at the end */
  }

  .sidebar-source a {
    word-break: break-all; /* or break-word */
    overflow-wrap: anywhere;
  }

</style>
