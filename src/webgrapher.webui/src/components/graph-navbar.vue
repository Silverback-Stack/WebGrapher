<template>
  <header>
    <b-navbar type="is-primary">
      <template #brand>
        <b-navbar-item @click="$emit('reset-graph')">
          <span class="has-text-weight-bold is-size-4">WebGrapher</span>
          <span v-if="graphTitle"
                class="is-size-4 has-text-grey has-text-weight-light">
            <span class="icon"><i class="mdi mdi-chevron-right"></i></span>
            {{ graphTitle }}
          </span>
        </b-navbar-item>
      </template>

      <template #end>
        <!--Crawl-->
        <b-navbar-item v-if="signalrStatus === 'connected'"
                       href="#" @click="$emit('open-crawl')">
          <span class="icon"><i class="mdi mdi-spider"></i></span>
          <span>Crawl</span>
        </b-navbar-item>

        <!--Connect-->
        <b-navbar-item href="#" @click="$emit('open-connect')">
          <span class="icon" v-if="signalrStatus === 'connected'">
            <i class="mdi mdi-broadcast"></i>
          </span>
          <span class="icon" v-else>
            <i class="mdi mdi-broadcast-off"></i>
          </span>
          <span>{{ signalrStatus === 'connected' ? "Connected" : "Connect" }}</span>
        </b-navbar-item>

        <!--Activity-->
        <b-navbar-item v-if="signalrStatus === 'connected'"
                       href="#" @click="$emit('open-activity')">
          <span class="icon"><i class="mdi mdi-list-box"></i></span>
          <span>Activity</span>
        </b-navbar-item>

        <!--Dropdown menu-->
        <b-navbar-dropdown label="">
          <b-navbar-item href="#" @click="$emit('open-create')">
            New Graph
          </b-navbar-item>

          <b-navbar-item v-if="signalrStatus === 'connected'"
                         href="#" @click="$emit('open-update')">
            Graph Settings
          </b-navbar-item>

          <hr class="navbar-divider">

          <b-navbar-item v-if="!isAuthenticated"
                         tag="router-link"
                         :to="{ name: 'Login' }">
            Login
          </b-navbar-item>

          <b-navbar-item v-if="isAuthenticated"
                         tag="router-link"
                         :to="{ name: 'Logout' }">
            Logout
          </b-navbar-item>

        </b-navbar-dropdown>

      </template>
    </b-navbar>
  </header>
</template>

<style>
  .navbar {
    border-radius: 0 !important;
    z-index: 2000 !important; /* position above graph and side bars but under modal */
  }

  .navbar .navbar-dropdown {
    box-shadow: 0 8px 8px rgba(10, 10, 10, .1) !important;
    /* alight menu to right */
    right: 0 !important;
    left: auto !important;
  }

  /* Restore background color of menu items */
  .navbar .navbar-dropdown .navbar-item {
    background-color: inherit !important;
  }

  /*
    Override bulmaswatch/flatly styles back to defaults - prevents text from dissapearing/colour on smaller screens
  */
  .navbar-brand .navbar-item {
    color: inherit !important;
  }

  /* Restore square corners of navbar menu */
  .navbar .navbar-menu {
    border-radius: inherit !important;
  }
</style>

<script setup>
  defineProps({
    signalrStatus: String,
    graphTitle: String,
    isAuthenticated: Boolean
  })

  defineEmits([
    "reset-graph",
    "open-crawl",
    "open-connect",
    "open-create",
    "open-activity"
  ])
</script>
