<template>
  <nav class="card">
    <header class="card-header">
      <p class="card-header-title is-size-4">New WebMap</p>
    </header>

    <div class="card-content scrollable-content">
      <section>
        <b-field label="Name">
          <b-input model-value=""></b-input>
        </b-field>
        <b-field label="Description">
          <b-input model-value=""></b-input>
        </b-field>
        <b-field label="Seed Url">
          <b-input type="url" model-value="https://"></b-input>
        </b-field>
        <b-field label="Max Depth">
          <b-slider size="is-large" :model-value="3" :min="1" :max="10"> </b-slider>
        </b-field>
        <b-field label="Max Links">
          <b-slider size="is-large" :model-value="50" :min="1" :max="100"> </b-slider>
        </b-field>

        <b-field>
          <b-checkbox model-value="true">Exclude query strings</b-checkbox>
        </b-field>
        <b-field>
          <b-checkbox model-value="true">Exclude external links</b-checkbox>
        </b-field>

        <b-field label="Url Filter">
          <p class="control">
            <span class="button is-static">RegEx</span>
          </p>
          <p class="control is-expanded">
            <b-input></b-input>
          </p>
        </b-field>
        <b-field label="Title">
          <p class="control">
            <span class="button is-static">XPath</span>
          </p>
          <p class="control is-expanded">
            <b-input></b-input>
          </p>
        </b-field>
        <b-field label="Image">
          <p class="control">
            <span class="button is-static">XPath</span>
          </p>
          <p class="control is-expanded">
            <b-input></b-input>
          </p>
        </b-field>
        <b-field label="Content">
          <p class="control">
            <span class="button is-static">XPath</span>
          </p>
          <p class="control is-expanded">
            <b-input></b-input>
          </p>
        </b-field>
        <b-field label="Summary">
          <p class="control">
            <span class="button is-static">XPath</span>
          </p>
          <p class="control is-expanded">
            <b-input></b-input>
          </p>
        </b-field>
        <b-field label="Related Links">
          <p class="control">
            <span class="button is-static">XPath</span>
          </p>
          <p class="control is-expanded">
            <b-input></b-input>
          </p>
        </b-field>
      </section>
    </div>

    <footer class="card-footer p-3 is-justify-content-center">
      <b-button type="is-primary" outlined @click="previewCrawl" class="mr-2">
        <span class="icon">
          <i class="mdi mdi-plus-thick"></i>
        </span>
        <span>Preview</span>
      </b-button>
      <b-button type="is-primary" outlined @click="previewCrawl">
        <span class="icon">
          <i class="mdi mdi-plus-thick"></i>
        </span>
        <span>Create</span>
      </b-button>
    </footer>
  </nav>
</template>

<script setup>
</script>

<style scoped>
</style>
