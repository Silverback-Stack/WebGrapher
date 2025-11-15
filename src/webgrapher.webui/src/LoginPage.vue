<template>
  <section class="login-page">
    <div class="login-box card">
      <div class="card-content">
        <h1 class="title has-text-centered mb-5">WebGrapher</h1>
        <h2 class="has-text-centered mb-5">
          WebGrapher is a distributed, scalable, event-driven microservices platform that crawls web pages, extracts and graphs relational data, and streams it live for real-time visualisation.
        </h2>

        <b-field label="Username">
          <b-input v-model="username" required placeholder="Enter your username"></b-input>
        </b-field>

        <b-field label="Password">
          <b-input v-model="password"
                   type="password"
                   required
                   placeholder="Enter your password"></b-input>
        </b-field>

        <div class="has-text-centered mt-5">
          <b-button type="is-primary"
                    size="is-medium"
                    @click="login">
            Login
          </b-button>
        </div>

        <p v-if="error" class="has-text-danger has-text-centered mt-4">
          {{ error }}
        </p>
      </div>
    </div>
  </section>
</template>

<style scoped>
  .login-page {
    background-image: url('./assets/login-background-hd.jpg');
    background-size: cover; /* stretch to fill */
    background-position: center; /* center image */
    background-repeat: no-repeat; /* don’t tile */
    min-height: 100vh; /* full viewport height */

    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
  }

  /* Centered login box */
  .login-box {
    width: 600px;
    padding: 2.5rem;
  }
</style>

<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiClient from './apiClient.js'
import apiConfig from "./config/api-config.js"

const router = useRouter()
const route = useRoute()

const username = ref('')
const password = ref('')
const error = ref('')


async function login() {
  error.value = ''

  try {

    // Call local login API
    // Note: local login flow exists only for testing
    // In production, users will authenticate through Azure AD or Auth0,
    // which handle their own login pages, access tokens, and refresh tokens.
    const response = await apiClient.post(apiConfig.AUTH_LOGIN, {
      username: username.value,
      password: password.value
    })

    // Store JWT and expiry tokens
    const { token, expires, username: returnedUsername } = response.data
    localStorage.setItem('jwt', token)
    localStorage.setItem('jwt_expires', expires)
    localStorage.setItem('username', returnedUsername)

    // Redirect back to the page user was trying to access
    const redirectTo = route.query.redirect || '/'
    router.push(redirectTo)
  } catch (err) {
    console.error('Login failed', err)
    error.value = 'Login failed. Please check your credentials.'
  }
}
</script>
