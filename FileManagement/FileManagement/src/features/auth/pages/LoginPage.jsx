import { LoginShowcase } from '../components/LoginShowcase.jsx'
import { LoginForm } from '../components/LoginForm.jsx'

export function LoginPage(props) {
  return (
    <main className="auth-screen">
      <LoginShowcase />
      <LoginForm {...props} />
    </main>
  )
}
