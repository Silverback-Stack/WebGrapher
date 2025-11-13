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
      const logoutUrl = data.logoutUrl
      const callbackUrl = `${window.location.origin}/callback`

      // Store provider and logoutUrl for later use (logout page)
      if (logoutUrl) {
        // Encode return url
        const returnUrl = encodeURIComponent(window.location.origin)

        // Replace placeholder with encoded redirect URL
        const returnToUrl = logoutUrl.replace("{return_url}", returnUrl)

        localStorage.setItem('logoutUrl', returnToUrl)
        localStorage.setItem('authProvider', provider)
      }

      if (provider === 'Local') {
        // Local login flow
        const currentRoute = router.currentRoute.value
        router.push({
          name: 'Login',
          query: { redirect: currentRoute.fullPath }
        })
      }
      else if (loginUrl) { // External provider (AzureAD, Auth0, etc.)

        // Encode callback url
        const redirectUrl = encodeURIComponent(callbackUrl);

        // Replace placeholder with encoded redirect URL
        const loginProviderUrl = loginUrl.replace("{callback_url}", redirectUrl)

        console.log(`Redirecting to ${provider} login:`, loginProviderUrl)
        window.location.href = loginProviderUrl
      }
      else {
        console.warn('Unauthorized with no login URL provided for redirect.')
      }
    }

    return Promise.reject(error)
  }
)

export default apiClient
