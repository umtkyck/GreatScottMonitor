import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { Auth0Provider } from '@auth0/auth0-react';
import Dashboard from './pages/Dashboard';
import UserManagement from './pages/UserManagement';
import PolicyConfiguration from './pages/PolicyConfiguration';
import AuditLog from './pages/AuditLog';
import Layout from './components/Layout';

const domain = process.env.REACT_APP_AUTH0_DOMAIN || '';
const clientId = process.env.REACT_APP_AUTH0_CLIENT_ID || '';
const audience = process.env.REACT_APP_AUTH0_AUDIENCE || '';

function App() {
  return (
    <Auth0Provider
      domain={domain}
      clientId={clientId}
      audience={audience}
      redirectUri={window.location.origin}
    >
      <Router>
        <Layout>
          <Routes>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/users" element={<UserManagement />} />
            <Route path="/policies" element={<PolicyConfiguration />} />
            <Route path="/audit-logs" element={<AuditLog />} />
          </Routes>
        </Layout>
      </Router>
    </Auth0Provider>
  );
}

export default App;

