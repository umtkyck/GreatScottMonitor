import React from 'react';
import { useAuth0 } from '@auth0/auth0-react';
import { Link, useLocation } from 'react-router-dom';
import './Layout.css';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const { isAuthenticated, loginWithRedirect, logout, user } = useAuth0();
  const location = useLocation();

  if (!isAuthenticated) {
    return (
      <div className="login-container">
        <div className="login-box">
          <h1>MedSecure Vision Admin</h1>
          <p>Please log in to continue</p>
          <button onClick={() => loginWithRedirect()}>Log In</button>
        </div>
      </div>
    );
  }

  return (
    <div className="layout">
      <nav className="sidebar">
        <div className="sidebar-header">
          <h2>MedSecure Vision</h2>
        </div>
        <ul className="nav-menu">
          <li className={location.pathname === '/dashboard' ? 'active' : ''}>
            <Link to="/dashboard">Dashboard</Link>
          </li>
          <li className={location.pathname === '/users' ? 'active' : ''}>
            <Link to="/users">User Management</Link>
          </li>
          <li className={location.pathname === '/policies' ? 'active' : ''}>
            <Link to="/policies">Policy Configuration</Link>
          </li>
          <li className={location.pathname === '/audit-logs' ? 'active' : ''}>
            <Link to="/audit-logs">Audit Logs</Link>
          </li>
        </ul>
        <div className="sidebar-footer">
          <div className="user-info">
            <p>{user?.name || user?.email}</p>
          </div>
          <button onClick={() => logout({ returnTo: window.location.origin })}>
            Logout
          </button>
        </div>
      </nav>
      <main className="main-content">
        {children}
      </main>
    </div>
  );
};

export default Layout;


