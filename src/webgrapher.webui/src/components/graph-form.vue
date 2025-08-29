<template>
  <nav class="card">
    <header class="card-header">
      <p class="card-header-title is-size-4">
        {{ mode === "create" ? "New Graph" : mode === "update" ? "Default Settings" : "Crawl Page" }}
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

        <!-- Only show Name & Description in create/update -->
        <b-field v-if="mode !== 'crawl'" label="Name">
          <b-input v-model="form.name" required></b-input>
        </b-field>
        <b-field v-if="mode !== 'crawl'" label="Description">
          <b-input v-model="form.description" required></b-input>
        </b-field>

        <hr v-if="mode !== 'crawl'" />

        <!-- Always show Url, Max Links & Max Depth -->
        <b-field :label="mode === 'crawl' ? 'Url' : 'Seed Url'">
          <b-input v-model="form.url" type="url" required></b-input>
        </b-field>
        <b-field label="Max Links">
          <b-slider v-model="form.maxLinks" size="is-large" :min="1" :max="50"></b-slider>
        </b-field>
        <b-field label="Max Depth">
          <b-slider v-model="form.maxDepth" ticks size="is-large" :min="1" :max="3"></b-slider>
        </b-field>
        <p class="has-text-grey">
          <span>Potentially {{ potentialRequests.toLocaleString() }} crawl page requests.</span>
        </p>

        <hr v-if="mode !== 'crawl'" />

        <!-- Advanced Crawl Options (hidden by default in crawl mode) -->
        <!-- Toggle link only in crawl mode -->
        <div v-if="mode === 'crawl'" class="my-2">
          <a @click="showAdvancedOptions = !showAdvancedOptions" class="is-clickable has-text-link">
            Advanced Settings
            <b-icon :icon="showAdvancedOptions ? 'chevron-down' : 'chevron-right'" size="is-small" />
          </a>
        </div>

        <!-- Advanced controls -->
        <div v-show="showAdvancedOptions">
          <b-field label="Url Filter">
            <p class="control">
              <span class="button is-static">RegEx</span>
            </p>
            <p class="control is-expanded">
              <b-input v-model="form.urlMatchRegex"></b-input>
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
            <b-checkbox v-model="form.excludeQueryStrings">
              Exclude query strings
            </b-checkbox>
          </b-field>
          <b-field>
            <b-checkbox v-model="form.excludeExternalLinks">
              Exclude external links
            </b-checkbox>
          </b-field>

          <hr v-if="mode === 'crawl'" />

          <b-field v-if="mode === 'crawl'">
            <b-checkbox v-model="form.overwriteDefaults">
              Overwrite default settings
            </b-checkbox>
          </b-field>
        </div>

      </section>
    </div>

    <footer class="card-footer p-3 is-justify-content-center">
      <b-button v-if="mode !== 'update'" type="is-primary"
                outlined @click="preview"
                class="mr-2"
                :disabled="isSubmitting">
        <span class="icon">
          <i class="mdi mdi-bullseye"></i>
        </span>
        <span>Preview</span>
      </b-button>

      <b-button type="is-primary"
                outlined
                @click="submitForm"
                :disabled="isSubmitting">
        <span class="icon"><i class="mdi mdi-check-outline"></i></span>
        <span>
          {{ mode === "create" ? "Create" : mode === "update" ? "Save Defaults" : "Crawl" }}
        </span>
      </b-button>
    </footer>
  </nav>
</template>

<script setup>
  import { ref, reactive, watch, watchEffect, onMounted, computed } from "vue"
  import { useRouter } from "vue-router"
  import axios from "axios"
  import apiConfig from "../api-config.js"

  const router = useRouter()

  const props = defineProps({
    graphId: { type: String, default: null },
    mode: { type: String, default: "create" }, // "create" | "update" | "crawl"
    crawlUrl: { type: String, default: "" } 
  });
  const { mode, graphId } = props;
  const showAdvancedOptions = ref(mode !== 'crawl');

  const emit = defineEmits(["confirmAction"])

  const errorMessages = ref([])
  const cardContent = ref(null)
  const isSubmitting = ref(false)

  // form model
  const form = reactive({
    name: "",
    description: "",
    url: "https://",
    maxLinks: 10,
    maxDepth: 1,
    excludeExternalLinks: true,
    excludeQueryStrings: true,
    urlMatchRegex: "",
    titleElementXPath: "",
    contentElementXPath: "",
    summaryElementXPath: "",
    imageElementXPath: "",
    relatedLinksElementXPath: "",
    overwriteDefaults: false
  })


  // Compute the potential requests as a geometric sum
  const potentialMaxRequests = 127550
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
    showAdvancedOptions.value = mode !== 'crawl';
  });


  // Load existing graph if graphId is provided
  onMounted(async () => {
    if (!props.graphId) return;

    try {
      const response = await axios.get(
        apiConfig.GRAPH_GET(props.graphId)
      );
      Object.assign(form, response.data);

      // If we're crawling, override just the URL after properties load
      if (mode === "crawl" && props.crawlUrl) {
        form.url = props.crawlUrl;
      }

    } catch (err) {
      errorMessages.value = ["Failed to load graph data."];
      console.error(err);
    }
  });

  // Sumbit (Create | Update | Crawl)
  async function submitForm() {
    if (isSubmitting.value) return;

    errorMessages.value = [];
    isSubmitting.value = true;

    try {
      let response;

      if (mode === "create") {
        response = await axios.post(apiConfig.GRAPH_CREATE, form, {
          headers: { "Content-Type": "application/json" },
        })

        const newGraph = response.data;
        emit("confirmAction", newGraph);

        // Set route url to allow App.vue to handle the SignalR connection via route watcher
        await router.push({ name: "Graph", params: { id: newGraph.id } })

      } else if (mode === "update") {
        response = await axios.put(apiConfig.GRAPH_UPDATE(props.graphId), form, {
          headers: { "Content-Type": "application/json" },
        })

        emit("confirmAction", response.data);
      } else if (mode === "crawl") {
        response = await axios.post(apiConfig.GRAPH_CRAWL(props.graphId), form, {
          headers: { "Content-Type": "application/json" },
        })
        emit("confirmAction", response.data);
      }

    } catch (err) {
      if (err.response?.data) {
        errorMessages.value = extractErrorMessages(err.response.data);
      } else {
        errorMessages.value = ["Something went wrong."];
      }
      console.error(err);

    } finally {
      isSubmitting.value = false
    }
  }


  function extractErrorMessages(apiError) {
    if (!apiError?.errors) return [apiError?.title || "Unknown error"];
    return Object.values(apiError.errors).flat();
  }

</script>

<style scoped>
</style>
