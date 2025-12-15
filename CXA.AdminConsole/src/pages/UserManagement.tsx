import React, { useEffect, useState } from 'react';
import axios from 'axios';
import './UserManagement.css';

interface User {
  userId: string;
  email: string;
  name: string;
  role: string;
  status: string;
  enrolledAt: string | null;
  lastActive: string | null;
}

const UserManagement: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    try {
      const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:5001';
      const response = await axios.get(`${apiUrl}/api/users`);
      setUsers(response.data);
    } catch (error) {
      console.error('Error fetching users:', error);
    } finally {
      setLoading(false);
    }
  };

  const filteredUsers = users.filter(user =>
    user.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
    user.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const handleSuspend = async (userId: string) => {
    try {
      const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:5001';
      await axios.post(`${apiUrl}/api/users/${userId}/suspend`);
      fetchUsers();
    } catch (error) {
      console.error('Error suspending user:', error);
    }
  };

  const handleActivate = async (userId: string) => {
    try {
      const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:5001';
      await axios.post(`${apiUrl}/api/users/${userId}/activate`);
      fetchUsers();
    } catch (error) {
      console.error('Error activating user:', error);
    }
  };

  const handleReEnroll = async (userId: string) => {
    try {
      const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:5001';
      await axios.post(`${apiUrl}/api/users/${userId}/re-enroll`);
      alert('Re-enrollment initiated');
    } catch (error) {
      console.error('Error initiating re-enrollment:', error);
    }
  };

  if (loading) {
    return <div className="loading">Loading users...</div>;
  }

  return (
    <div className="user-management">
      <div className="page-header">
        <h1>User Management</h1>
        <input
          type="text"
          placeholder="Search users..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="search-input"
        />
      </div>

      <table className="users-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Role</th>
            <th>Status</th>
            <th>Enrolled</th>
            <th>Last Active</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {filteredUsers.map(user => (
            <tr key={user.userId}>
              <td>{user.name}</td>
              <td>{user.email}</td>
              <td>{user.role || 'User'}</td>
              <td>
                <span className={`status-badge ${user.status}`}>
                  {user.status}
                </span>
              </td>
              <td>{user.enrolledAt ? new Date(user.enrolledAt).toLocaleDateString() : 'Not enrolled'}</td>
              <td>{user.lastActive ? new Date(user.lastActive).toLocaleString() : 'Never'}</td>
              <td>
                <div className="action-buttons">
                  {user.status === 'active' ? (
                    <button onClick={() => handleSuspend(user.userId)} className="btn-suspend">
                      Suspend
                    </button>
                  ) : (
                    <button onClick={() => handleActivate(user.userId)} className="btn-activate">
                      Activate
                    </button>
                  )}
                  <button onClick={() => handleReEnroll(user.userId)} className="btn-reenroll">
                    Re-enroll
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default UserManagement;






