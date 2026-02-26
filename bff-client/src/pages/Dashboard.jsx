import { useState, useEffect } from 'react';
import '../styles/Dashboard.css';

function Dashboard() {
  const [time, setTime] = useState(new Date());

  useEffect(() => {
    const timer = setInterval(() => setTime(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  const stats = [
    { label: 'Total Users', value: '12,345', change: '+12.5%', positive: true },
    { label: 'Revenue', value: '$45.2K', change: '+8.3%', positive: true },
    { label: 'Active Sessions', value: '892', change: '-3.2%', positive: false },
    { label: 'Conversion Rate', value: '3.24%', change: '+1.8%', positive: true },
  ];

  const recentActivity = [
    { user: 'John Doe', action: 'Completed transaction', time: '2m ago' },
    { user: 'Jane Smith', action: 'Updated profile', time: '5m ago' },
    { user: 'Mike Johnson', action: 'New registration', time: '12m ago' },
    { user: 'Sarah Williams', action: 'Password reset', time: '18m ago' },
    { user: 'Tom Brown', action: 'Document uploaded', time: '23m ago' },
  ];

  return (
    <div className="dashboard">
      {/* Header */}
      <header className="dashboard-header">
        <div className="header-content">
          <div>
            <h1 className="dashboard-title">Dashboard</h1>
            <p className="dashboard-subtitle">Welcome back! Here's what's happening today.</p>
          </div>
          <div className="header-time">
            <div className="time-display">
              {time.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}
            </div>
            <div className="date-display">
              {time.toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric' })}
            </div>
          </div>
        </div>
      </header>

      {/* Stats Grid */}
      <div className="stats-grid">
        {stats.map((stat, index) => (
          <div key={index} className="stat-card">
            <div className="stat-card-inner">
              <div className="stat-label">{stat.label}</div>
              <div className="stat-value">{stat.value}</div>
              <div className={`stat-change ${stat.positive ? 'positive' : 'negative'}`}>
                <span className="change-arrow">{stat.positive ? '↑' : '↓'}</span>
                {stat.change}
              </div>
            </div>
            <div className="stat-icon">
              <div className="pulse-ring"></div>
            </div>
          </div>
        ))}
      </div>

      {/* Main Content Grid */}
      <div className="content-grid">
        {/* Chart Section */}
        <div className="chart-card glass-card">
          <h3 className="card-title">Analytics Overview</h3>
          <div className="chart-container">
            <div className="bar-chart">
              {[65, 45, 85, 55, 75, 90, 70, 60, 80, 95, 50, 85].map((height, index) => (
                <div key={index} className="bar-wrapper">
                  <div 
                    className="bar" 
                    style={{ 
                      height: `${height}%`,
                      animationDelay: `${index * 0.1}s`
                    }}
                  ></div>
                  <div className="bar-label">{index + 1}</div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Activity Feed */}
        <div className="activity-card glass-card">
          <h3 className="card-title">Recent Activity</h3>
          <div className="activity-list">
            {recentActivity.map((activity, index) => (
              <div key={index} className="activity-item">
                <div className="activity-avatar">
                  {activity.user.split(' ').map(n => n[0]).join('')}
                </div>
                <div className="activity-content">
                  <div className="activity-user">{activity.user}</div>
                  <div className="activity-action">{activity.action}</div>
                </div>
                <div className="activity-time">{activity.time}</div>
              </div>
            ))}
          </div>
        </div>

        {/* Quick Actions */}
        <div className="quick-actions glass-card">
          <h3 className="card-title">Quick Actions</h3>
          <div className="actions-grid">
            <button className="action-button">
              <span className="action-icon">📊</span>
              <span>Generate Report</span>
            </button>
            <button className="action-button">
              <span className="action-icon">👥</span>
              <span>Add User</span>
            </button>
            <button className="action-button">
              <span className="action-icon">⚙️</span>
              <span>Settings</span>
            </button>
            <button className="action-button">
              <span className="action-icon">📧</span>
              <span>Send Email</span>
            </button>
          </div>
        </div>

        {/* System Status */}
        <div className="status-card glass-card">
          <h3 className="card-title">System Status</h3>
          <div className="status-list">
            <div className="status-item">
              <div className="status-label">
                <span className="status-dot online"></span>
                API Server
              </div>
              <div className="status-value">Online</div>
            </div>
            <div className="status-item">
              <div className="status-label">
                <span className="status-dot online"></span>
                Database
              </div>
              <div className="status-value">Healthy</div>
            </div>
            <div className="status-item">
              <div className="status-label">
                <span className="status-dot warning"></span>
                Cache Service
              </div>
              <div className="status-value">Warning</div>
            </div>
            <div className="status-item">
              <div className="status-label">
                <span className="status-dot online"></span>
                Storage
              </div>
              <div className="status-value">78% Used</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default Dashboard;
