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

  // Redirect to home page
  router.replace({ path: '/' })
})
</script>
