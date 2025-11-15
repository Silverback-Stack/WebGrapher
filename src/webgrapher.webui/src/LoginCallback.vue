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
      // Decode JWT to read claims
      const claims = decodeJwt(token)

      // Extract username
      const username = getUsernameFromClaims(claims)

      // Store in localStorage
      localStorage.setItem('jwt', token)
      localStorage.setItem('username', username ?? '')

      router.replace('/')
    } else {
      console.error('Login callback did not contain token.', params)
      router.replace('/login')
    }

  })

  function decodeJwt(token) {
    const [, payload] = token.split('.')
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(decoded)
  }

  function getUsernameFromClaims(claims) {
    return (
      claims.preferred_username ||
      claims.name ||
      claims.email ||
      claims.nickname ||
      claims.sub
    )
  }
</script>


