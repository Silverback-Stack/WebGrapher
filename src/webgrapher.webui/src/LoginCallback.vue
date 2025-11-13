<template>
  <div>
    <p>Processing login...</p>
  </div>
</template>

<script setup>
  import { onMounted } from 'vue'
  import { useRouter, useRoute } from 'vue-router'

  // Utility to parse query string or hash params
  function parseQueryString(queryString) {
    const params = new URLSearchParams(queryString)
    const result = {}
    for (const [key, value] of params.entries()) {
      result[key] = value
    }
    return result
  }

  const router = useRouter()
  const route = useRoute()

  onMounted(() => {
    // AzureAD / Auth0 can return token in query or hash fragment
    const hash = window.location.hash.substring(1) // remove '#'
    const query = window.location.search.substring(1) // remove '?'
    const params = hash ? parseQueryString(hash) : parseQueryString(query)

    // Extract token (depends on provider, e.g., 'id_token' or 'access_token')
    const token = params.access_token || params.id_token

    if (token) {
      // Store token in localStorage for apiClient
      localStorage.setItem('jwt', token)
      // Redirect back to home
      router.replace('/')

    } else {
      console.error('Login callback did not contain token.', params)
      router.replace('/login')
    }
  })
</script>


