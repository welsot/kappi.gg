import { useState } from 'react';
import { Form, redirect, useActionData, useNavigate, useSearchParams } from 'react-router';
import type { Route } from './+types/login';
import { Navbar } from '~/components/Navbar';
import { Footer } from '~/components/Footer';
import { apiUsersRegister, apiUsersLogin } from '~/api/apiComponents';
import { setAuthCookies } from '~/utils/auth.server';

export async function action({ request }: Route.ActionArgs) {
  const formData = await request.formData();
  const step = formData.get('step') as string;
  const email = formData.get('email') as string;
  const code = formData.get('code') as string;
  const redirectTo = formData.get('redirectTo') as string;

  if (step === '1') {
    // Step 1: Send code to email
    try {
      await apiUsersRegister({ body: { email: email.trim() } });
      return { success: true, step: 1, email };
    } catch (error) {
      return { success: false, step: 1, error: 'Failed to send verification code' };
    }
  } else if (step === '2') {
    // Step 2: Verify code and login
    try {
      const response = await apiUsersLogin({
        body: {
          email: email.trim(),
          code: code.trim(),
        },
      });

      // Set auth cookies
      const cookieHeaders = setAuthCookies(response);
      const headers = new Headers();
      cookieHeaders.forEach((cookie) => {
        headers.append('Set-Cookie', cookie);
      });

      // Redirect to the original page or dashboard
      const destination = redirectTo && redirectTo !== '/login' && redirectTo !== '/register'
        ? redirectTo
        : '/dashboard';

      return redirect(destination, { headers });
    } catch (error) {
      return { success: false, step: 2, error: 'Invalid verification code', email };
    }
  }

  return { success: false, step: 1, error: 'Invalid request' };
}

export default function Login() {
  const actionData = useActionData<typeof action>();
  const [searchParams] = useSearchParams();
  const redirectTo = searchParams.get('redirectTo') || '/dashboard';
  const [currentStep, setCurrentStep] = useState<number>(
    actionData?.step === 2 ? 2 : 1
  );
  const [email, setEmail] = useState<string>(actionData?.email || '');

  // Update step when action data changes
  if (actionData?.success && actionData.step === 1 && currentStep === 1) {
    setCurrentStep(2);
    if (actionData.email) {
      setEmail(actionData.email);
    }
  }

  return (
    <div className="flex flex-col min-h-screen">
      <Navbar />

      <main className="flex-grow bg-gradient-to-b from-purple-50 to-white">
        <div className="container mx-auto px-4 py-16 md:py-24">
          <div className="max-w-md mx-auto bg-white p-8 rounded-2xl shadow-lg border border-purple-100">
            <h1 className="text-3xl font-bold text-gray-900 mb-2">Log In</h1>
            <p className="text-gray-600 mb-8">
              {currentStep === 1
                ? 'Enter your email to receive a verification code'
                : 'Enter the verification code sent to your email'}
            </p>

            {actionData?.error && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded-lg">
                {actionData.error}
              </div>
            )}

            {currentStep === 1 ? (
              <Step1Form redirectTo={redirectTo} />
            ) : (
              <Step2Form email={email} redirectTo={redirectTo} />
            )}
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}

function Step1Form({ redirectTo }: { redirectTo: string }) {
  const [email, setEmail] = useState('');

  return (
    <Form method="post">
      <input type="hidden" name="step" value="1" />
      <input type="hidden" name="redirectTo" value={redirectTo} />

      <div className="mb-6">
        <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-2">
          Email Address
        </label>
        <input
          type="email"
          id="email"
          name="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent"
          placeholder="you@example.com"
        />
      </div>

      <button
        type="submit"
        disabled={!email.trim()}
        className="w-full bg-purple-600 hover:bg-purple-700 disabled:bg-gray-300 disabled:cursor-not-allowed text-white font-semibold py-3 px-4 rounded-lg transition-colors duration-200"
      >
        Continue
      </button>

      <p className="mt-4 text-center text-sm text-gray-600">
        Don't have an account?{' '}
        <a href={`/register?redirectTo=${redirectTo}`} className="text-purple-600 hover:text-purple-700 font-medium">
          Create one
        </a>
      </p>
    </Form>
  );
}

function Step2Form({ email, redirectTo }: { email: string; redirectTo: string }) {
  const [code, setCode] = useState('');

  return (
    <Form method="post">
      <input type="hidden" name="step" value="2" />
      <input type="hidden" name="email" value={email} />
      <input type="hidden" name="redirectTo" value={redirectTo} />

      <div className="mb-4">
        <label htmlFor="email-readonly" className="block text-sm font-medium text-gray-700 mb-2">
          Email Address
        </label>
        <input
          type="email"
          id="email-readonly"
          value={email}
          readOnly
          className="w-full px-4 py-3 border border-gray-200 rounded-lg bg-gray-50 text-gray-600"
        />
      </div>

      <div className="mb-6">
        <label htmlFor="code" className="block text-sm font-medium text-gray-700 mb-2">
          Verification Code
        </label>
        <input
          type="text"
          id="code"
          name="code"
          value={code}
          onChange={(e) => setCode(e.target.value)}
          maxLength={6}
          required
          className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent text-center text-2xl tracking-widest font-mono"
          placeholder="000000"
        />
        <p className="mt-2 text-sm text-gray-500">
          Check your email for the 6-digit verification code
        </p>
      </div>

      <button
        type="submit"
        disabled={!code.trim() || code.trim().length !== 6}
        className="w-full bg-purple-600 hover:bg-purple-700 disabled:bg-gray-300 disabled:cursor-not-allowed text-white font-semibold py-3 px-4 rounded-lg transition-colors duration-200"
      >
        Log In
      </button>

      <p className="mt-4 text-center text-sm text-gray-600">
        Didn't receive the code?{' '}
        <button
          type="button"
          onClick={() => window.location.reload()}
          className="text-purple-600 hover:text-purple-700 font-medium"
        >
          Try again
        </button>
      </p>
    </Form>
  );
}
