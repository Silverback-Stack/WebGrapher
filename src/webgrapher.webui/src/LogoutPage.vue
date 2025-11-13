<template>
  <div>Logging out...</div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

onMounted(() => {
  // Clear JWT token from localStorage
  localStorage.removeItem('jwt')

  const logoutUrl = localStorage.getItem('logoutUrl')

  if (logoutUrl) {
    window.location.href = logoutUrl
  } else {
    router.replace('/')
  }

  // Clean up stored info
  localStorage.removeItem('logoutUrl')
  localStorage.removeItem('authProvider')
})
</script>
