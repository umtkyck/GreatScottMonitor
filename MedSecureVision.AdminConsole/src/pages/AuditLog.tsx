import React, { useEffect, useState } from 'react';
import axios from 'axios';
import './AuditLog.css';

interface AuditLogEntry {
  logId: string;
  eventType: string;
  timestamp: string;
  userId: string | null;
  workstationId: string | null;
  result: string | null;
  confidenceScore: number | null;
  failureReason: string | null;
}

const AuditLog: React.FC = () => {
  const [logs, setLogs] = useState<AuditLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [eventType, setEventType] = useState('');

  useEffect(() => {
    fetchAuditLogs();
  }, [startDate, endDate, eventType]);

  const fetchAuditLogs = async () => {
    try {
      const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:5001';
      const params: any = {};
      if (startDate) params.startDate = startDate;
      if (endDate) params.endDate = endDate;
      if (eventType) params.eventType = eventType;

      const response = await axios.get(`${apiUrl}/api/audit-logs`, { params });
      setLogs(response.data);
    } catch (error) {
      console.error('Error fetching audit logs:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleExport = async () => {
    try {
      const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:5001';
      const params: any = {};
      if (startDate) params.startDate = startDate;
      if (endDate) params.endDate = endDate;

      const response = await axios.get(`${apiUrl}/api/audit-logs/export`, {
        params,
        responseType: 'blob'
      });

      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `audit-logs-${new Date().toISOString().split('T')[0]}.csv`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (error) {
      console.error('Error exporting audit logs:', error);
    }
  };

  if (loading) {
    return <div className="loading">Loading audit logs...</div>;
  }

  return (
    <div className="audit-log">
      <div className="page-header">
        <h1>Audit Logs</h1>
        <button onClick={handleExport} className="export-button">
          Export CSV
        </button>
      </div>

      <div className="filters">
        <div className="filter-group">
          <label>Start Date</label>
          <input
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
          />
        </div>
        <div className="filter-group">
          <label>End Date</label>
          <input
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
          />
        </div>
        <div className="filter-group">
          <label>Event Type</label>
          <select value={eventType} onChange={(e) => setEventType(e.target.value)}>
            <option value="">All</option>
            <option value="authentication">Authentication</option>
            <option value="enrollment">Enrollment</option>
            <option value="lock">Lock</option>
            <option value="unlock">Unlock</option>
            <option value="admin_action">Admin Action</option>
            <option value="security_alert">Security Alert</option>
          </select>
        </div>
      </div>

      <div className="logs-table-container">
        <table className="logs-table">
          <thead>
            <tr>
              <th>Timestamp</th>
              <th>Event Type</th>
              <th>User ID</th>
              <th>Workstation</th>
              <th>Result</th>
              <th>Confidence</th>
              <th>Failure Reason</th>
            </tr>
          </thead>
          <tbody>
            {logs.map(log => (
              <tr key={log.logId}>
                <td>{new Date(log.timestamp).toLocaleString()}</td>
                <td>{log.eventType}</td>
                <td>{log.userId || '-'}</td>
                <td>{log.workstationId || '-'}</td>
                <td>
                  <span className={`result-badge ${log.result}`}>
                    {log.result || '-'}
                  </span>
                </td>
                <td>{log.confidenceScore ? log.confidenceScore.toFixed(2) : '-'}</td>
                <td>{log.failureReason || '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default AuditLog;


