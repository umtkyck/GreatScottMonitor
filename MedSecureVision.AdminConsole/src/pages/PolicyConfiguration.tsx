import React, { useEffect, useState } from 'react';
import axios from 'axios';
import './PolicyConfiguration.css';

interface PolicyConfig {
  absenceTimeoutSeconds: number;
  retryAttemptsBeforeLockout: number;
  fallbackMethods: string[];
  multiFacePolicy: 'warn' | 'lock';
  workingHoursStart: string;
  workingHoursEnd: string;
}

const PolicyConfiguration: React.FC = () => {
  const [config, setConfig] = useState<PolicyConfig>({
    absenceTimeoutSeconds: 5,
    retryAttemptsBeforeLockout: 3,
    fallbackMethods: ['pin', 'windows_hello'],
    multiFacePolicy: 'lock',
    workingHoursStart: '08:00',
    workingHoursEnd: '17:00'
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    fetchPolicyConfig();
  }, []);

  const fetchPolicyConfig = async () => {
    try {
      const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:5001';
      const response = await axios.get(`${apiUrl}/api/policies`);
      setConfig(response.data);
    } catch (error) {
      console.error('Error fetching policy config:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:5001';
      await axios.post(`${apiUrl}/api/policies`, config);
      alert('Policy configuration saved successfully');
    } catch (error) {
      console.error('Error saving policy config:', error);
      alert('Error saving policy configuration');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="loading">Loading policy configuration...</div>;
  }

  return (
    <div className="policy-configuration">
      <div className="page-header">
        <h1>Policy Configuration</h1>
        <button onClick={handleSave} disabled={saving} className="save-button">
          {saving ? 'Saving...' : 'Save Changes'}
        </button>
      </div>

      <div className="policy-sections">
        <section className="policy-section">
          <h2>Presence Monitoring</h2>
          <div className="form-group">
            <label>Absence Timeout (seconds)</label>
            <input
              type="number"
              min="3"
              max="30"
              value={config.absenceTimeoutSeconds}
              onChange={(e) => setConfig({ ...config, absenceTimeoutSeconds: parseInt(e.target.value) })}
            />
            <small>Time before locking when no face is detected (3-30 seconds)</small>
          </div>
        </section>

        <section className="policy-section">
          <h2>Authentication</h2>
          <div className="form-group">
            <label>Retry Attempts Before Lockout</label>
            <input
              type="number"
              min="1"
              max="10"
              value={config.retryAttemptsBeforeLockout}
              onChange={(e) => setConfig({ ...config, retryAttemptsBeforeLockout: parseInt(e.target.value) })}
            />
          </div>
        </section>

        <section className="policy-section">
          <h2>Fallback Authentication</h2>
          <div className="form-group">
            <label>Enabled Methods</label>
            <div className="checkbox-group">
              <label>
                <input
                  type="checkbox"
                  checked={config.fallbackMethods.includes('pin')}
                  onChange={(e) => {
                    if (e.target.checked) {
                      setConfig({ ...config, fallbackMethods: [...config.fallbackMethods, 'pin'] });
                    } else {
                      setConfig({ ...config, fallbackMethods: config.fallbackMethods.filter(m => m !== 'pin') });
                    }
                  }}
                />
                PIN Code
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={config.fallbackMethods.includes('windows_hello')}
                  onChange={(e) => {
                    if (e.target.checked) {
                      setConfig({ ...config, fallbackMethods: [...config.fallbackMethods, 'windows_hello'] });
                    } else {
                      setConfig({ ...config, fallbackMethods: config.fallbackMethods.filter(m => m !== 'windows_hello') });
                    }
                  }}
                />
                Windows Hello
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={config.fallbackMethods.includes('smart_card')}
                  onChange={(e) => {
                    if (e.target.checked) {
                      setConfig({ ...config, fallbackMethods: [...config.fallbackMethods, 'smart_card'] });
                    } else {
                      setConfig({ ...config, fallbackMethods: config.fallbackMethods.filter(m => m !== 'smart_card') });
                    }
                  }}
                />
                Smart Card
              </label>
            </div>
          </div>
        </section>

        <section className="policy-section">
          <h2>Security</h2>
          <div className="form-group">
            <label>Multiple Faces Policy</label>
            <select
              value={config.multiFacePolicy}
              onChange={(e) => setConfig({ ...config, multiFacePolicy: e.target.value as 'warn' | 'lock' })}
            >
              <option value="warn">Warn</option>
              <option value="lock">Lock Immediately</option>
            </select>
          </div>
        </section>

        <section className="policy-section">
          <h2>Working Hours</h2>
          <div className="form-group">
            <label>Start Time</label>
            <input
              type="time"
              value={config.workingHoursStart}
              onChange={(e) => setConfig({ ...config, workingHoursStart: e.target.value })}
            />
          </div>
          <div className="form-group">
            <label>End Time</label>
            <input
              type="time"
              value={config.workingHoursEnd}
              onChange={(e) => setConfig({ ...config, workingHoursEnd: e.target.value })}
            />
          </div>
        </section>
      </div>
    </div>
  );
};

export default PolicyConfiguration;






