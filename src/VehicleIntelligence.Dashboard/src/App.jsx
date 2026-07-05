import React, { useEffect, useState, useRef } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { 
  MapContainer, 
  TileLayer, 
  Marker, 
  Popup, 
  useMap 
} from 'react-leaflet';
import L from 'leaflet';
import { 
  Activity, 
  AlertTriangle, 
  Battery, 
  Gauge, 
  MapPin, 
  Navigation, 
  ShieldAlert, 
  Thermometer, 
  Zap,
  Filter,
  Info,
  Radio
} from 'lucide-react';
// API Base URL Configuration

const API_BASE_URL = 'https://localhost:7084';

// Helper to create custom glowing Leaflet markers based on Risk Score
const createHtmlMarker = (riskScore, vehicleId) => {
  let color = '#10b981'; // Green
  let shadow = 'rgba(16, 185, 129, 0.6)';
  
  if (riskScore >= 60) {
    color = '#ef4444'; // Red
    shadow = 'rgba(239, 68, 68, 0.8)';
  } else if (riskScore >= 30) {
    color = '#f59e0b'; // Orange/Amber
    shadow = 'rgba(245, 158, 11, 0.7)';
  }

  const html = `
    <div style="position: relative; width: 24px; height: 24px;">
      <div style="
        position: absolute;
        width: 12px;
        height: 12px;
        background-color: ${color};
        border-radius: 50%;
        top: 6px;
        left: 6px;
        border: 2px solid #ffffff;
        box-shadow: 0 0 10px ${color};
        z-index: 2;
      "></div>
      <div style="
        position: absolute;
        width: 24px;
        height: 24px;
        background-color: ${color};
        opacity: 0.4;
        border-radius: 50%;
        animation: pulse 1.5s infinite;
        box-shadow: 0 0 15px ${shadow};
        z-index: 1;
      "></div>
    </div>
    <style>
      @keyframes pulse {
        0% { transform: scale(0.6); opacity: 0.8; }
        100% { transform: scale(1.4); opacity: 0; }
      }
    </style>
  `;

  return L.divIcon({
    html: html,
    className: 'custom-gps-marker',
    iconSize: [24, 24],
    iconAnchor: [12, 12]
  });
};

// Component to dynamically update map center when selected vehicle moves
function ChangeMapCenter({ center }) {
  const map = useMap();
  useEffect(() => {
    if (center) {
      map.setView(center, map.getZoom());
    }
  }, [center, map]);
  return null;
}

export default function App() {
  const [vehicles, setVehicles] = useState({});
  const [selectedVehicleId, setSelectedVehicleId] = useState(null);
  const [alerts, setAlerts] = useState([]);
  const [telemetryHistory, setTelemetryHistory] = useState({});
  const [connectionState, setConnectionState] = useState('Disconnected');
  const [activeTab, setActiveTab] = useState('telemetry'); // telemetry | raw
  
  // UX Refinements State
  const [filterAlertsBySelected, setFilterAlertsBySelected] = useState(false);
  const [onlyShowOnline, setOnlyShowOnline] = useState(false);

  const connectionRef = useRef(null);

  // Fetch Initial Data
  useEffect(() => {
    // 1. Fetch initial registered vehicles
    fetch(`${API_BASE_URL}/api/vehicles?pageSize=50`)
      .then(res => res.json())
      .then(data => {
        const initialVehicles = {};
        if (data.items) {
          data.items.forEach(v => {
            initialVehicles[v.externalId] = {
              id: v.id,
              externalId: v.externalId,
              status: v.status,
              speed: 0,
              batteryLevel: 100,
              latitude: 41.0082,
              longitude: 28.9784,
              riskScore: 0,
              lastUpdated: new Date(0) // Start as offline
            };
          });
          setVehicles(initialVehicles);
        }
      })
      .catch(err => console.error('Failed to load initial vehicles', err));

    // 2. Fetch initial alerts
    fetch(`${API_BASE_URL}/api/alerts?pageSize=50`)
      .then(res => res.json())
      .then(data => {
        if (data.items) {
          setAlerts(data.items);
        }
      })
      .catch(err => console.error('Failed to load initial alerts', err));
  }, []);

  // Connect to SignalR
  useEffect(() => {
    const hubConnection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/telemetry`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    hubConnection.on('ReceiveTelemetryUpdate', (telemetry) => {
      // 1. Update vehicle current status
      setVehicles(prev => {
        const current = prev[telemetry.vehicleExternalId] || {};
        return {
          ...prev,
          [telemetry.vehicleExternalId]: {
            ...current,
            id: telemetry.vehicleId,
            externalId: telemetry.vehicleExternalId,
            tripId: telemetry.tripId,
            speed: telemetry.speed,
            latitude: telemetry.latitude,
            longitude: telemetry.longitude,
            batteryLevel: telemetry.batteryLevel,
            temperature: telemetry.temperature,
            riskScore: telemetry.riskScore,
            engineRpm: telemetry.engineRpm,
            engineLoad: telemetry.engineLoad,
            fuelRate: telemetry.fuelRate,
            energyConsumption: telemetry.energyConsumption,
            batteryVoltage: telemetry.batteryVoltage,
            batteryCurrent: telemetry.batteryCurrent,
            elevation: telemetry.elevation,
            speedLimit: telemetry.speedLimit,
            rawPayloadJson: telemetry.rawPayloadJson,
            lastUpdated: new Date() // Set to now to indicate online status
          }
        };
      });

      // 2. Append to telemetry history for charts
      setTelemetryHistory(prev => {
        const history = prev[telemetry.vehicleExternalId] || [];
        const newHistory = [...history, {
          timestamp: new Date(telemetry.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' }),
          speed: telemetry.speed,
          speedLimit: telemetry.speedLimit || 90.0
        }];
        // Keep last 30 data points
        if (newHistory.length > 30) {
          newHistory.shift();
        }
        return {
          ...prev,
          [telemetry.vehicleExternalId]: newHistory
        };
      });
    });

    hubConnection.on('ReceiveAlert', (alert) => {
      // 1. Add alert to feed list
      setAlerts(prev => [alert, ...prev.slice(0, 99)]); // keep last 100 alerts
    });

    hubConnection.start()
      .then(() => {
        setConnectionState('Connected');
      })
      .catch(err => {
        setConnectionState('Failed');
        console.error('SignalR Connection Error: ', err);
      });

    connectionRef.current = hubConnection;

    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, []);

  // Sort stably by external ID (numerically if possible) to prevent items from shifting around
  const vehicleList = Object.values(vehicles).sort((a, b) => {
    const aNum = parseFloat(a.externalId);
    const bNum = parseFloat(b.externalId);
    if (!isNaN(aNum) && !isNaN(bNum)) {
      return aNum - bNum;
    }
    return a.externalId.localeCompare(b.externalId);
  });

  const displayedVehicles = onlyShowOnline 
    ? vehicleList.filter(v => (new Date() - v.lastUpdated) < 15000)
    : vehicleList;

  const onlineVehiclesCount = vehicleList.filter(v => (new Date() - v.lastUpdated) < 15000).length;

  // Auto-select the first online vehicle if current is offline or none selected
  useEffect(() => {
    const activeVehicles = vehicleList.filter(v => (new Date() - v.lastUpdated) < 15000);
    if (activeVehicles.length > 0) {
      const isCurrentActive = selectedVehicleId && (new Date() - (vehicles[selectedVehicleId]?.lastUpdated || 0)) < 15000;
      if (!selectedVehicleId || !isCurrentActive) {
        setSelectedVehicleId(activeVehicles[0].externalId);
      }
    }
  }, [vehicles, selectedVehicleId]);

  const selectedVehicle = vehicles[selectedVehicleId];
  const historyData = selectedVehicleId ? (telemetryHistory[selectedVehicleId] || []) : [];

  // Filter alerts by selected vehicle if toggle is active
  const displayedAlerts = filterAlertsBySelected
    ? alerts.filter(a => a.vehicleExternalId === selectedVehicleId)
    : alerts;

  // Map settings
  const mapCenter = selectedVehicle && selectedVehicle.latitude !== 41.0082 
    ? [selectedVehicle.latitude, selectedVehicle.longitude] 
    : [41.0082, 28.9784];

  const isSelectedVehicleOnline = selectedVehicle && (new Date() - selectedVehicle.lastUpdated) < 15000;

  return (
    <div className="glass-container">
      {/* ─────────────────────────────────────────────
          Header Panel
          ───────────────────────────────────────────── */}
      <header className="glass-header">
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <Activity size={24} style={{ color: '#3b82f6' }} className="glow-active" />
          <h1 style={{ margin: 0, fontSize: '18px', fontWeight: 700 }} className="text-gradient">
            VEHICLE INTELLIGENCE PLATFORM
          </h1>
        </div>

        {/* Dynamic Warning for Simulator */}
        {onlineVehiclesCount === 0 && (
          <div style={{ 
            display: 'flex', 
            alignItems: 'center', 
            gap: '8px', 
            background: 'rgba(245, 158, 11, 0.12)', 
            border: '1px solid rgba(245, 158, 11, 0.3)',
            padding: '6px 14px',
            borderRadius: '8px',
            color: '#f59e0b',
            fontSize: '11px',
            fontWeight: 500
          }}>
            <AlertTriangle size={14} />
            <span>Waiting for Simulator stream... Run Simulator console to see live movement!</span>
          </div>
        )}

        <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span style={{ fontSize: '12px', color: '#9ca3af' }}>SignalR:</span>
            <span className={`badge ${connectionState === 'Connected' ? 'badge-success' : 'badge-danger'}`}>
              <Zap size={10} /> {connectionState}
            </span>
          </div>

          <div className="glass-card" style={{ padding: '6px 14px', borderRadius: '20px' }}>
            <span style={{ fontSize: '12px', fontWeight: 600, color: '#f3f4f6' }}>
              Online Vehicles: {onlineVehiclesCount} / {vehicleList.length}
            </span>
          </div>
        </div>
      </header>

      {/* ─────────────────────────────────────────────
          Main Dashboard Work Area
          ───────────────────────────────────────────── */}
      <div className="glass-content">
        {/* Sidebar: Vehicles List with Filtering */}
        <aside className="glass-sidebar">
          <div style={{ 
            padding: '16px', 
            borderBottom: '1px solid var(--border-color)',
            display: 'flex',
            flexDirection: 'column',
            gap: '10px'
          }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2 style={{ fontSize: '13px', fontWeight: 700, margin: 0, color: '#f3f4f6' }}>VEHICLES REGISTERED</h2>
              <span style={{ fontSize: '10px', color: '#9ca3af', background: 'rgba(255,255,255,0.05)', padding: '2px 6px', borderRadius: '4px' }}>
                Online: {onlineVehiclesCount}
              </span>
            </div>

            {/* Filter Toggle */}
            <label style={{ 
              display: 'flex', 
              alignItems: 'center', 
              gap: '8px', 
              fontSize: '11px', 
              color: '#9ca3af',
              cursor: 'pointer',
              background: onlyShowOnline ? 'rgba(59, 130, 246, 0.08)' : 'rgba(255,255,255,0.02)',
              padding: '6px 10px',
              borderRadius: '6px',
              border: onlyShowOnline ? '1px solid rgba(59, 130, 246, 0.3)' : '1px solid var(--border-color)',
              transition: 'all 0.2s'
            }}>
              <input 
                type="checkbox" 
                checked={onlyShowOnline} 
                onChange={(e) => setOnlyShowOnline(e.target.checked)}
                style={{ cursor: 'pointer' }}
              />
              <Filter size={12} style={{ color: onlyShowOnline ? '#3b82f6' : '#9ca3af' }} />
              <strong>Show Online Vehicles Only</strong>
            </label>
          </div>

          {/* Sidebar Vehicle List */}
          <div style={{ 
            display: 'flex', 
            flexDirection: 'column', 
            gap: '8px', 
            padding: '12px',
            overflowY: 'auto',
            flex: 1
          }}>
            {displayedVehicles.length === 0 ? (
              <div style={{ textAlign: 'center', color: '#6b7280', fontSize: '12px', padding: '24px 12px' }}>
                {onlyShowOnline ? 'No vehicles currently online. Turn off the filter or start the simulator.' : 'No registered vehicles.'}
              </div>
            ) : (
              displayedVehicles.map(vehicle => {
                const isActive = (new Date() - vehicle.lastUpdated) < 15000;
                const isSelected = selectedVehicleId === vehicle.externalId;
                
                let riskBadge = 'badge-success';
                if (vehicle.riskScore >= 60) riskBadge = 'badge-danger';
                else if (vehicle.riskScore >= 30) riskBadge = 'badge-warning';

                return (
                  <div 
                    key={vehicle.externalId} 
                    className={`glass-card ${isSelected ? 'active' : ''}`}
                    style={{ 
                      cursor: 'pointer', 
                      padding: '12px',
                      borderLeft: isActive ? '3px solid #10b981' : '1px solid var(--border-color)',
                      boxShadow: isActive ? '0 0 10px rgba(16, 185, 129, 0.1)' : 'none'
                    }}
                    onClick={() => setSelectedVehicleId(vehicle.externalId)}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                        {isActive ? (
                          <span style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: '#10b981', display: 'inline-block' }} className="glow-active"></span>
                        ) : (
                          <span style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: '#4b5563', display: 'inline-block' }}></span>
                        )}
                        <span style={{ fontWeight: 700, fontSize: '14px', color: isSelected ? '#3b82f6' : '#f3f4f6' }}>
                          {vehicle.externalId}
                        </span>
                      </div>
                      <span className={`badge ${riskBadge}`}>Risk: {Math.round(vehicle.riskScore)}</span>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '11px', color: '#9ca3af' }}>
                      <span>Speed: {Math.round(vehicle.speed)} km/h</span>
                      <span>Battery: {Math.round(vehicle.batteryLevel)}%</span>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '10px', color: '#6b7280', marginTop: '6px' }}>
                      <span>Status: {isActive ? 'Online' : 'Offline'}</span>
                      <span>Last: {vehicle.lastUpdated.getTime() === 0 ? 'Never' : vehicle.lastUpdated.toLocaleTimeString()}</span>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </aside>

        {/* Main Content Area */}
        <main className="glass-main">
          {/* Top Panel: Map & Alerts */}
          <div className="glass-panel-top">
            {/* Live Map Panel */}
            <div className="glass-map-container">
              <MapContainer 
                center={mapCenter} 
                zoom={14} 
                scrollWheelZoom={false}
              >
                <TileLayer
                  url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                />
                {selectedVehicle && selectedVehicle.latitude !== 41.0082 && (
                  <ChangeMapCenter center={[selectedVehicle.latitude, selectedVehicle.longitude]} />
                )}
                {vehicleList.map(v => {
                  const isActive = (new Date() - v.lastUpdated) < 15000;
                  if (!isActive && onlyShowOnline) return null; // Skip rendering offline markers if filtered
                  return (
                    <Marker 
                      key={v.externalId} 
                      position={[v.latitude, v.longitude]} 
                      icon={createHtmlMarker(v.riskScore, v.externalId)}
                    >
                      <Popup>
                        <div style={{ fontSize: '12px' }}>
                          <strong style={{ fontSize: '14px' }}>{v.externalId}</strong>
                          <hr style={{ margin: '6px 0', borderColor: 'rgba(255,255,255,0.1)' }} />
                          Speed: {Math.round(v.speed)} km/h<br />
                          Battery: {Math.round(v.batteryLevel)}%<br />
                          Risk Score: {Math.round(v.riskScore)}<br />
                          Elevation: {v.elevation ? `${Math.round(v.elevation)} m` : 'N/A'}<br />
                          Last Active: {v.lastUpdated.getTime() === 0 ? 'Never' : v.lastUpdated.toLocaleTimeString()}
                        </div>
                      </Popup>
                    </Marker>
                  );
                })}
              </MapContainer>
            </div>

            {/* Live Alerts Panel with Selected Vehicle Filter */}
            <div className="glass-alerts-panel">
              <div style={{ 
                padding: '16px', 
                borderBottom: '1px solid var(--border-color)', 
                display: 'flex', 
                flexDirection: 'column',
                gap: '8px'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <ShieldAlert size={16} style={{ color: '#ef4444' }} />
                  <h2 style={{ fontSize: '13px', fontWeight: 700, margin: 0, color: '#f3f4f6' }}>LIVE ALERTS FEED</h2>
                </div>

                {/* Alerts Filter Checkbox */}
                <label style={{ 
                  display: 'flex', 
                  alignItems: 'center', 
                  gap: '6px', 
                  fontSize: '10px', 
                  color: '#9ca3af',
                  cursor: 'pointer',
                  background: filterAlertsBySelected ? 'rgba(239, 68, 68, 0.08)' : 'rgba(255,255,255,0.02)',
                  padding: '4px 8px',
                  borderRadius: '4px',
                  border: filterAlertsBySelected ? '1px solid rgba(239, 68, 68, 0.3)' : '1px solid var(--border-color)',
                  width: 'fit-content'
                }}>
                  <input 
                    type="checkbox" 
                    checked={filterAlertsBySelected} 
                    onChange={(e) => setFilterAlertsBySelected(e.target.checked)}
                    style={{ cursor: 'pointer' }}
                  />
                  <span>Show Selected Vehicle Alerts Only ({selectedVehicleId || 'None'})</span>
                </label>
              </div>

              <div style={{ padding: '12px', display: 'flex', flexDirection: 'column', gap: '8px', overflowY: 'auto', flex: 1 }}>
                {displayedAlerts.length === 0 ? (
                  <div style={{ textAlign: 'center', color: '#6b7280', fontSize: '11px', marginTop: '24px' }}>
                    {filterAlertsBySelected 
                      ? `No alerts for selected vehicle ${selectedVehicleId}.` 
                      : 'No alerts triggered yet. System healthy.'}
                  </div>
                ) : (
                  displayedAlerts.map((alert, index) => {
                    let alertSeverityClass = 'badge-success';
                    if (alert.severity === 'Critical') alertSeverityClass = 'badge-danger';
                    else if (alert.severity === 'High') alertSeverityClass = 'badge-danger';
                    else if (alert.severity === 'Medium') alertSeverityClass = 'badge-warning';

                    const isCurrentSelected = alert.vehicleExternalId === selectedVehicleId;

                    return (
                      <div 
                        key={alert.alertId || index} 
                        className="glass-card" 
                        style={{ 
                          padding: '10px', 
                          borderLeft: alert.severity === 'Critical' ? '3px solid var(--color-danger)' : alert.severity === 'High' ? '3px solid var(--color-danger)' : '1px solid var(--border-color)',
                          border: isCurrentSelected ? '1px solid rgba(59, 130, 246, 0.5)' : '1px solid var(--border-color)'
                        }}
                      >
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' }}>
                          <span style={{ fontWeight: 700, fontSize: '12px', color: isCurrentSelected ? '#3b82f6' : '#f3f4f6' }}>
                            {alert.vehicleExternalId}
                          </span>
                          <span className={`badge ${alertSeverityClass}`} style={{ fontSize: '9px', padding: '2px 6px' }}>{alert.severity}</span>
                        </div>
                        <p style={{ fontSize: '11px', color: '#d1d5db', margin: '4px 0' }}>{alert.message}</p>
                        <span style={{ fontSize: '9px', color: '#6b7280' }}>{new Date(alert.timestamp).toLocaleTimeString()}</span>
                      </div>
                    );
                  })
                )}
              </div>
            </div>
          </div>

          {/* Bottom Panel: Telemetry Info */}
          <div className="glass-stats-panel">
            {/* Quick Metrics Panels (1x4 Grid) */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: '12px', height: '100%' }}>
              {selectedVehicle ? (
                <>
                  {/* Battery Level Card */}
                  <div className="glass-card" style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', padding: '16px', gap: '8px', position: 'relative', height: '100%' }}>
                    <div style={{ position: 'absolute', top: '8px', right: '12px', display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <span style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: isSelectedVehicleOnline ? '#10b981' : '#6b7280' }} className={isSelectedVehicleOnline ? 'glow-active' : ''}></span>
                      <span style={{ fontSize: '10px', color: '#9ca3af' }}>{isSelectedVehicleOnline ? 'Live' : 'Cached'}</span>
                    </div>
                    <Battery size={28} style={{ color: selectedVehicle.batteryLevel < 20 ? '#ef4444' : '#10b981' }} />
                    <span style={{ fontSize: '12px', color: '#9ca3af', fontWeight: 600, letterSpacing: '0.05em' }}>BATTERY LEVEL</span>
                    <span style={{ fontSize: '28px', fontWeight: 800, color: '#f3f4f6' }}>{Math.round(selectedVehicle.batteryLevel)}%</span>
                    <span style={{ fontSize: '11px', color: '#6b7280' }}>
                      {selectedVehicle.batteryVoltage ? `${selectedVehicle.batteryVoltage.toFixed(1)}V` : ''} 
                      {selectedVehicle.batteryCurrent ? ` | ${selectedVehicle.batteryCurrent.toFixed(1)}A` : ''}
                    </span>
                  </div>

                  {/* Temp Card */}
                  <div className="glass-card" style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', padding: '16px', gap: '8px', position: 'relative', height: '100%' }}>
                    <div style={{ position: 'absolute', top: '8px', right: '12px', display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <span style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: isSelectedVehicleOnline ? '#10b981' : '#6b7280' }} className={isSelectedVehicleOnline ? 'glow-active' : ''}></span>
                      <span style={{ fontSize: '10px', color: '#9ca3af' }}>{isSelectedVehicleOnline ? 'Live' : 'Cached'}</span>
                    </div>
                    <Thermometer size={28} style={{ color: selectedVehicle.temperature > 100 ? '#ef4444' : '#60a5fa' }} />
                    <span style={{ fontSize: '12px', color: '#9ca3af', fontWeight: 600, letterSpacing: '0.05em' }}>OUTSIDE TEMP</span>
                    <span style={{ fontSize: '28px', fontWeight: 800, color: '#f3f4f6' }}>{selectedVehicle.temperature ? `${selectedVehicle.temperature.toFixed(1)}°C` : 'N/A'}</span>
                    <span style={{ fontSize: '11px', color: '#6b7280' }}>
                      {selectedVehicle.elevation ? `Elevation: ${Math.round(selectedVehicle.elevation)}m` : 'Elevation: N/A'}
                    </span>
                  </div>

                  {/* Engine RPM & Load Card */}
                  <div className="glass-card" style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', padding: '16px', gap: '8px', position: 'relative', height: '100%' }}>
                    <div style={{ position: 'absolute', top: '8px', right: '12px', display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <span style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: isSelectedVehicleOnline ? '#10b981' : '#6b7280' }} className={isSelectedVehicleOnline ? 'glow-active' : ''}></span>
                      <span style={{ fontSize: '10px', color: '#9ca3af' }}>{isSelectedVehicleOnline ? 'Live' : 'Cached'}</span>
                    </div>
                    <Gauge size={28} style={{ color: '#f59e0b' }} />
                    <span style={{ fontSize: '12px', color: '#9ca3af', fontWeight: 600, letterSpacing: '0.05em' }}>ENGINE LOAD / RPM</span>
                    <span style={{ fontSize: '24px', fontWeight: 800, color: '#f3f4f6' }}>
                      {selectedVehicle.engineLoad ? `${Math.round(selectedVehicle.engineLoad)}%` : '0%'}
                    </span>
                    <span style={{ fontSize: '11px', color: '#6b7280' }}>
                      {selectedVehicle.engineRpm ? `${Math.round(selectedVehicle.engineRpm)} RPM` : '0 RPM'}
                    </span>
                  </div>

                  {/* Fuel Flow Card */}
                  <div className="glass-card" style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', padding: '16px', gap: '8px', position: 'relative', height: '100%' }}>
                    <div style={{ position: 'absolute', top: '8px', right: '12px', display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <span style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: isSelectedVehicleOnline ? '#10b981' : '#6b7280' }} className={isSelectedVehicleOnline ? 'glow-active' : ''}></span>
                      <span style={{ fontSize: '10px', color: '#9ca3af' }}>{isSelectedVehicleOnline ? 'Live' : 'Cached'}</span>
                    </div>
                    <Zap size={28} style={{ color: '#10b981' }} />
                    <span style={{ fontSize: '12px', color: '#9ca3af', fontWeight: 600, letterSpacing: '0.05em' }}>FUEL / MAF FLOW</span>
                    <span style={{ fontSize: '24px', fontWeight: 800, color: '#f3f4f6' }}>
                      {selectedVehicle.fuelRate ? `${selectedVehicle.fuelRate.toFixed(1)} L/h` : '0.0 L/h'}
                    </span>
                    <span style={{ fontSize: '11px', color: '#6b7280' }}>
                      MAF: {selectedVehicle.massAirFlow ? `${selectedVehicle.massAirFlow.toFixed(1)} g/s` : 'N/A'}
                    </span>
                  </div>
                </>
              ) : (
                <div className="glass-card" style={{ gridColumn: 'span 4', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', color: '#6b7280', fontSize: '12px', gap: '8px' }}>
                  <Radio size={24} style={{ color: '#4b5563' }} />
                  <span>Select a vehicle to view its metrics cards.</span>
                </div>
              )}
            </div>

            {/* Tabbed Info Panel */}
            <div className="glass-card" style={{ display: 'flex', flexDirection: 'column', height: '100%', padding: '12px', overflow: 'hidden' }}>
              <div style={{ display: 'flex', borderBottom: '1px solid var(--border-color)', marginBottom: '8px', paddingBottom: '4px', gap: '12px' }}>
                <button 
                  onClick={() => setActiveTab('telemetry')}
                  style={{ 
                    background: 'none', border: 'none', padding: '4px', cursor: 'pointer', fontSize: '11px', fontWeight: 600,
                    color: activeTab === 'telemetry' ? '#3b82f6' : '#6b7280',
                    borderBottom: activeTab === 'telemetry' ? '2px solid #3b82f6' : 'none'
                  }}
                >
                  AUDIT DETAILS
                </button>
                <button 
                  onClick={() => setActiveTab('raw')}
                  style={{ 
                    background: 'none', border: 'none', padding: '4px', cursor: 'pointer', fontSize: '11px', fontWeight: 600,
                    color: activeTab === 'raw' ? '#3b82f6' : '#6b7280',
                    borderBottom: activeTab === 'raw' ? '2px solid #3b82f6' : 'none'
                  }}
                >
                  RAW JSON
                </button>
              </div>

              <div style={{ flex: 1, overflowY: 'auto', fontSize: '10px', color: '#9ca3af', fontFamily: 'monospace' }}>
                {selectedVehicle ? (
                  activeTab === 'telemetry' ? (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                      <div><strong>Vehicle External ID:</strong> {selectedVehicle.externalId}</div>
                      <div><strong>Internal GUID:</strong> {selectedVehicle.id}</div>
                      <div><strong>Active Trip ID:</strong> {selectedVehicle.tripId || 'N/A'}</div>
                      <div><strong>GPS Location:</strong> {selectedVehicle.latitude.toFixed(6)}, {selectedVehicle.longitude.toFixed(6)}</div>
                      <div><strong>Risk Score calculated:</strong> {selectedVehicle.riskScore.toFixed(2)} / 100</div>
                      <div><strong>HVAC AC Power:</strong> {selectedVehicle.airConditioningPower ? `${selectedVehicle.airConditioningPower.toFixed(2)} kW` : 'N/A'}</div>
                      <div><strong>HVAC Heater Power:</strong> {selectedVehicle.heaterPower ? `${selectedVehicle.heaterPower.toFixed(1)} Watts` : 'N/A'}</div>
                      <div><strong>Energy Consumption:</strong> {selectedVehicle.energyConsumption ? `${selectedVehicle.energyConsumption.toFixed(1)} Wh/km` : 'N/A'}</div>
                      <div><strong>Last Received:</strong> {selectedVehicle.lastUpdated.getTime() === 0 ? 'Never' : selectedVehicle.lastUpdated.toISOString()}</div>
                    </div>
                  ) : (
                    <pre style={{ margin: 0, whiteSpace: 'pre-wrap', wordBreak: 'break-all' }}>
                      {selectedVehicle.rawPayloadJson 
                        ? JSON.stringify(JSON.parse(selectedVehicle.rawPayloadJson), null, 2) 
                        : 'No raw payload available'}
                    </pre>
                  )
                ) : (
                  <div style={{ color: '#6b7280', textAlign: 'center', marginTop: '24px' }}>No vehicle selected</div>
                )}
              </div>
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}
