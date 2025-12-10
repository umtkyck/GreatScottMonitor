import React, { useEffect, useState } from 'react';
import { Line, Doughnut } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  ArcElement,
  Title,
  Tooltip,
  Legend
} from 'chart.js';
import axios from 'axios';
import './Dashboard.css';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  ArcElement,
  Title,
  Tooltip,
  Legend
);

interface DashboardStats {
  activeSessions: number;
  successRate24h: number;
  successRate7d: number;
  successRate30d: number;
  failedAttempts: number;
}

const Dashboard: React.FC = () => {
  const [stats, setStats] = useState<DashboardStats>({
    activeSessions: 0,
    successRate24h: 0,
    successRate7d: 0,
    successRate30d: 0,
    failedAttempts: 0
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchDashboardData();
  }, []);

  const fetchDashboardData = async () => {
    try {
      const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:5001';
      const response = await axios.get(`${apiUrl}/api/dashboard/stats`);
      setStats(response.data);
    } catch (error) {
      console.error('Error fetching dashboard data:', error);
    } finally {
      setLoading(false);
    }
  };

  const successRateData = {
    labels: ['24h', '7d', '30d'],
    datasets: [
      {
        label: 'Success Rate (%)',
        data: [stats.successRate24h, stats.successRate7d, stats.successRate30d],
        backgroundColor: ['#28a745', '#17a2b8', '#ffc107'],
        borderColor: ['#28a745', '#17a2b8', '#ffc107'],
        borderWidth: 1
      }
    ]
  };

  const failedAttemptsData = {
    labels: ['Today', 'This Week', 'This Month'],
    datasets: [
      {
        label: 'Failed Attempts',
        data: [stats.failedAttempts, stats.failedAttempts * 7, stats.failedAttempts * 30],
        borderColor: '#dc3545',
        backgroundColor: 'rgba(220, 53, 69, 0.1)',
        tension: 0.4
      }
    ]
  };

  if (loading) {
    return <div className="dashboard-loading">Loading dashboard...</div>;
  }

  return (
    <div className="dashboard">
      <h1>Dashboard</h1>
      
      <div className="stats-grid">
        <div className="stat-card">
          <h3>Active Sessions</h3>
          <p className="stat-value">{stats.activeSessions}</p>
        </div>
        <div className="stat-card">
          <h3>Success Rate (24h)</h3>
          <p className="stat-value">{stats.successRate24h.toFixed(1)}%</p>
        </div>
        <div className="stat-card">
          <h3>Success Rate (7d)</h3>
          <p className="stat-value">{stats.successRate7d.toFixed(1)}%</p>
        </div>
        <div className="stat-card">
          <h3>Success Rate (30d)</h3>
          <p className="stat-value">{stats.successRate30d.toFixed(1)}%</p>
        </div>
      </div>

      <div className="charts-grid">
        <div className="chart-card">
          <h3>Success Rates</h3>
          <Doughnut data={successRateData} />
        </div>
        <div className="chart-card">
          <h3>Failed Attempts Timeline</h3>
          <Line data={failedAttemptsData} />
        </div>
      </div>
    </div>
  );
};

export default Dashboard;

