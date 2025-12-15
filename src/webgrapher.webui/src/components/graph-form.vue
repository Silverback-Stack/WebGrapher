<template>
  <div class="card">
    <header class="card-header">
      <p class="card-header-title is-size-4">
        <span v-if="props.mode === 'create'">New Graph</span>
        <span v-else-if="props.mode === 'update'">Graph Settings</span>
        <span v-else-if="props.mode === 'crawl'">Crawl</span>
        <span v-else-if="props.mode === 'preview'">Preview</span>
      </p>
    </header>

    <div ref="cardContent" class="card-content scrollable-content">
      <section>
        <!-- Form Error -->
        <b-notification v-if="errorMessages.length"
                        type="is-danger"
                        has-icon
                        class="mb-4">
          <ul>
            <li v-for="(msg, index) in errorMessages" :key="index">{{ msg }}</li>
          </ul>
        </b-notification>

        <!-- Only show in create/update -->
        <div v-if="props.mode === 'create' || props.mode === 'update'">
          <b-field label="Name">
            <b-input v-model="form.name" required></b-input>
          </b-field>
          <b-field label="Description">
            <b-input v-model="form.description"></b-input>
          </b-field>
        </div>

        <hr v-if="props.mode === 'update'" />

        <!-- Only show in crawl/update -->
        <div v-if="props.mode === 'crawl' || props.mode === 'update'">
          <b-field label="Url" class="mb-4">
            <b-input v-model="form.url" type="url" required placeholder="https://"></b-input>
          </b-field>
          <b-field label="Max Links">
            <b-slider v-model="form.maxLinks" size="is-large" :min="1" :max="appConfig.crawlMaxLinks"></b-slider>
          </b-field>
          <b-field label="Max Depth">
            <b-slider v-model="form.maxDepth" ticks size="is-large" :min="1" :max="appConfig.crawlMaxDepth"></b-slider>
          </b-field>
          <p class="has-text-grey">
            <span>Potentially {{ potentialRequests.toLocaleString() }} crawl requests.</span>
          </p>

          <hr v-if="props.mode == 'update'" />

          <!-- Advanced Crawl Options (hidden by default in crawl mode) -->
          <!-- Toggle link only in crawl mode -->
          <div v-if="props.mode === 'crawl'" class="has-text-centered">
            <a @click="showAdvancedOptions = !showAdvancedOptions" class="is-clickable has-text-link">
              Advanced Settings
              <b-icon :icon="showAdvancedOptions ? 'chevron-down' : 'chevron-up'" size="is-small" />
            </a>
          </div>

          <!-- Advanced controls -->
          <div v-show="showAdvancedOptions">
            <b-field label="Url Filters">
              <p class="control">
                <b-tooltip label="Enter one RegEx per line. Each line is treated as a separate pattern." position="is-right">
                  <span class="button is-static">RegEx</span>
                </b-tooltip>
              </p>
              <p class="control is-expanded">
                <b-input v-model="form.urlMatchRegex"
                         type="textarea" rows="2"></b-input>
              </p>
            </b-field>

            <b-field label="Title Container">
              <p class="control">
                <span class="button is-static">XPath</span>
              </p>
              <p class="control is-expanded">
                <b-input v-model="form.titleElementXPath"></b-input>
              </p>
            </b-field>

            <b-field label="Image Container">
              <p class="control">
                <span class="button is-static">XPath</span>
              </p>
              <p class="control is-expanded">
                <b-input v-model="form.imageElementXPath"></b-input>
              </p>
            </b-field>

            <b-field label="Content Container">
              <p class="control">
                <span class="button is-static">XPath</span>
              </p>
              <p class="control is-expanded">
                <b-input v-model="form.contentElementXPath"></b-input>
              </p>
            </b-field>

            <b-field label="Summary Container">
              <p class="control">
                <span class="button is-static">XPath</span>
              </p>
              <p class="control is-expanded">
                <b-input v-model="form.summaryElementXPath"></b-input>
              </p>
            </b-field>

            <b-field label="Related Links Container">
              <p class="control">
                <span class="button is-static">XPath</span>
              </p>
              <p class="control is-expanded">
                <b-input v-model="form.relatedLinksElementXPath"></b-input>
              </p>
            </b-field>

            <b-field>
              <b-checkbox v-model="form.excludeExternalLinks">
                Exclude external links
              </b-checkbox>
            </b-field>

            <b-field>
              <b-checkbox v-model="form.excludeQueryStrings">
                Exclude query strings
              </b-checkbox>
            </b-field>

            <b-field>
              <b-checkbox v-model="form.consolidateQueryStrings">
                Consolidate query strings
              </b-checkbox>
            </b-field>

            <b-field v-if="props.mode === 'crawl'">
              <b-checkbox v-model="form.overwriteDefaults">
                Overwrite default settings
              </b-checkbox>
            </b-field>
          </div>

        </div>

        <div v-if="props.mode === 'preview'">

          <div v-if="previewLogs.length">

            <div v-for="log in previewLogs" :key="log.id">
              <p>
                <i v-if="log.service === 'Crawler'"
                   :class="[
                   'mdi mdi-spider icon',
                   log.type === 'Error' ? 'has-text-danger' : '',
                   log.type === 'Warning' ? 'has-text-warning' : ''
                 ]"></i>
                <i v-if="log.service === 'Scraper'"
                   :class="[
                   'mdi mdi-cloud-download icon',
                   log.type === 'Error' ? 'has-text-danger' : '',
                   log.type === 'Warning' ? 'has-text-warning' : ''
                 ]"></i>
                <i v-if="log.service === 'Normalisation'"
                   :class="[
                     'mdi mdi-text-box icon',
                     log.type === 'Error' ? 'has-text-danger' : '',
                     log.type === 'Warning' ? 'has-text-warning' : ''
                   ]"></i>
                <span>{{ log.message }}</span>
              </p>

              <!-- If this log is NormalisationSuccess, show preview details -->
              <div v-if="log.code === 'NormalisationSuccess' && log.context?.preview"
                   class="mt-2 pl-4 box">

                <div v-if="log.context.preview.imageUrl" class="columns is-mobile">
                  <div class="column is-3">
                    <b-image v-if="log.context.preview.imageUrl"
                             :src="log.context.preview.imageUrl"
                             alt="Preview"
                             responsive />
                  </div>
                </div>

                <h3><strong>Title</strong></h3>
                <p>{{ log.context.preview.title }}</p>

                <h3 class="mt-3"><strong>Summary</strong></h3>
                <p>{{ log.context.preview.summary }}</p>

                <h3 class="mt-3"><strong>Keywords</strong></h3>
                <p>{{ log.context.preview.keywords }}</p>

                <h3 class="mt-3"><strong>Tags</strong></h3>
                <p>
                  <span v-for="(tag, idx) in log.context.preview.tags" :key="idx">{{ tag }}, </span>
                </p>

                <h3 class="mt-3"><strong>Links</strong></h3>
                <ul>
                  <li v-for="(link, idx) in log.context.preview.links" :key="idx">
                    <a :href="link" target="_blank">{{ link }}</a>
                  </li>
                </ul>
              </div>

            </div>

          </div>

          <div v-if="previewLogs.length === 0 || (
               previewLogs.every(log => log.code !== 'NormalisationSuccess') &&
               previewLogs.every(log => log.type === 'Information'))"
               class="mt-2 pl-4 box has-text-centered">
            <span class="icon is-large">
              <i class="mdi mdi-loading mdi-spin mdi-48px"></i>
            </span>
            <p>Generating Preview</p>
          </div>

        </div>

      </section>
    </div>

    <footer class="card-footer p-3 is-justify-content-center">
      <!-- Preview Button -->
      <b-button v-if="props.mode === 'crawl'" type="is-primary"
                outlined
                @click="submitForm(true)"
                class="mr-2"
                :disabled="isSubmitting">
        <span class="icon">
          <i class="mdi mdi-bullseye"></i>
        </span>
        <span>Preview</span>
      </b-button>

      <!-- Create / Update / Crawl Button -->
      <b-button v-if="props.mode !== 'preview'"
                type="is-primary"
                outlined
                @click="submitForm()"
                :disabled="isSubmitting">
        <span v-if="props.mode === 'crawl'" class="icon">
          <i class="mdi mdi-spider icon"></i>
        </span>
        <span v-if="props.mode !== 'crawl'" class="icon">
          <i class="mdi mdi-check-outline"></i>
        </span>
        <span v-if="props.mode === 'create'">Create</span>
        <span v-else-if="props.mode === 'update'">Save</span>
        <span v-else-if="props.mode === 'crawl'">Crawl</span>
        <span v-else-if="props.mode === 'preview'">Preview</span>
      </b-button>

      <!-- Preview Back Button -->
      <b-button v-if="props.mode === 'preview'"
                type="is-primary"
                outlined
                class="mr-2"
                @click="$emit('previewBack')">
        <span class="icon">
          <i class="mdi mdi-arrow-left-circle"></i>
        </span>
        <span>Back</span>
      </b-button>
    </footer>
  </div>
</template>

<script setup>
  import { ref, reactive, watch, watchEffect, onMounted, computed } from "vue"
  import { useRouter } from "vue-router"
  import axios from "axios"
  import apiClient from '../apiClient.js'
  import appConfig from "../config/app-config.js"
  import apiConfig from "../config/api-config.js"

  const router = useRouter()

  const props = defineProps({
    graphId: { type: String, default: null },
    correlationId: { type: String, default: null },
    mode: { type: String, default: "create" }, // "create" | "update" | "crawl" | "preview"
    crawlUrl: { type: String, default: "https://" },
    activityLogs: { type: Array, default: [] }
  });
  const { graphId } = props;
  const showAdvancedOptions = ref(props.mode !== 'crawl');

  const emit = defineEmits(["confirmAction", "previewBack"])

  const errorMessages = ref([])
  const cardContent = ref(null)
  const isSubmitting = ref(false)

  // form model
  const form = reactive({
    name: "",
    description: "",
    url: "https://",
    maxLinks: 1,
    maxDepth: 1,
    excludeExternalLinks: true,
    excludeQueryStrings: true,
    consolidateQueryStrings: true,
    urlMatchRegex: "",
    titleElementXPath: "",
    contentElementXPath: "",
    summaryElementXPath: "",
    imageElementXPath: "",
    relatedLinksElementXPath: "",
    overwriteDefaults: true,
    preview: false
  })


  // Compute the potential requests as a geometric sum
  const potentialRequests = computed(() => {
    let total = 0
    for (let d = 1; d <= form.maxDepth; d++) {
      total += Math.pow(form.maxLinks, d)
    }
    return total
  })


  // Scroll to top on errors
  watch(errorMessages, (newVal) => {
    if (newVal.length && cardContent.value) {
      cardContent.value.scrollTop = 0;
    }
  });

  // Monitor if Mode changes
  watchEffect(() => {
    showAdvancedOptions.value = props.mode !== 'crawl';
  });


  // Load existing graph if graphId is provided
  onMounted(async () => {
    if (!props.graphId) {
      if (!form.url) form.url = "https://";
      return;
    }

    try {
      const response = await apiClient.get(
        apiConfig.GRAPH_GET(props.graphId)
      );
      Object.assign(form, response.data);

      // If we're crawling, override just the URL after properties load
      if (props.mode === "crawl" && props.crawlUrl) {
        form.url = props.crawlUrl;
      } else if (!form.url) {
        form.url = "https://";
      }

    } catch (err) {
      errorMessages.value = ["Failed to load graph data."];
      console.error(err);
    }
  });


  // Sumbit (Create | Update | Crawl )
  async function submitForm(preview = false) {
    if (isSubmitting.value) return;

    errorMessages.value = [];
    isSubmitting.value = true;

    try {
      let response;

      if (props.mode === "create") {
        response = await apiClient.post(apiConfig.GRAPH_CREATE, form, {
          headers: { "Content-Type": "application/json" },
        })

        // Emit the newly created graph object
        emit("confirmAction", {
          type: "create",
          graph: response.data
        });

        // Set route url to allow App.vue to handle the SignalR connection via route watcher
        await router.push({ name: "Graph", params: { id: response.data.id } })

      } else if (props.mode === "update") {
        response = await apiClient.put(apiConfig.GRAPH_UPDATE(props.graphId), form, {
          headers: { "Content-Type": "application/json" },
        })

        emit("confirmAction", {
          type: "update",
          graph: response.data
        });

      } else if (props.mode === "crawl") {
        form.preview = preview
        response = await apiClient.post(apiConfig.GRAPH_CRAWL(props.graphId), form, {
          headers: { "Content-Type": "application/json" },
        })

        emit("confirmAction", {
          type: "crawl",
          data: response.data
        });

      }
    } catch (err) {
      console.error("Error submitting form:", err);

      const data = err.response?.data;

      if (data) {
        // Handle both text and JSON error payloads
        errorMessages.value = extractErrorMessages(data);

      } else if (err.message) {
        // Handle network or unexpected errors
        errorMessages.value = [err.message];

      } else {
        errorMessages.value = ["Something went wrong."];
      }

    } finally {
      isSubmitting.value = false
    }
  }


  function extractErrorMessages(apiError) {
    if (!apiError?.errors) return [apiError?.title || apiError || "Unknown error"];
    return Object.values(apiError.errors).flat();
  }

  // Find the matching preview logs
  const previewLogs = computed(() => {
    return props.activityLogs.filter(
      log =>
        log.graphId === props.graphId &&
        log.correlationId === props.correlationId
    )
    .sort((a, b) => new Date(a.timestamp) - new Date(b.timestamp)) // oldest first
  })

</script>

<style scoped>
</style>
