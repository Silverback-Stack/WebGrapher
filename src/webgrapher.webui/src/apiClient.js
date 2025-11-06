import axios from 'axios'
import router from './router'

const apiClient = axios.create({
  timeout: 15000
})

// Attach JWT token to every request
apiClient.interceptors.request.use(config => {
  const token = localStorage.getItem('jwt')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

//  Global Unauthorized 401 handler
apiClient.interceptors.response.use(
  response => response,
  error => {
    const status = error.response?.status
    const data = error.response?.data

    if (status === 401 && data) {
      const provider = data.identityProvider
      const loginUrl = data.loginUrl
      const currentUrl = window.location.href

      if (provider === 'Local') {
        // Local login flow
        const currentRoute = router.currentRoute.value
        router.push({
          name: 'Login',
          query: { redirect: currentRoute.fullPath }
        })
      }
      else if (loginUrl) {
        // External provider (AzureAD, Auth0, etc.)
        const encodedReturnUrl = encodeURIComponent(currentUrl)
        const redirectTo = `${loginUrl}?redirect_uri=${encodedReturnUrl}`
        console.log(`Redirecting to ${provider} login:`, redirectTo)
        window.location.href = redirectTo
      }
      else {
        console.warn('Unauthorized with no login URL provided for redirect.')
      }
    }

    return Promise.reject(error)
  }
)

export default apiClient
