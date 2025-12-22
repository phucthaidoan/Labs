import { messaging, getToken, onMessage } from './firebase-config.js';
import {
    registerDeviceToken,
    sendNotification,
    sendNotificationWithData,
    scheduleNotification,
    getUserScheduledNotifications,
    cancelScheduledNotification,
    getUserNotificationHistory,
    getNotificationStats
} from './api-service.js';

// VAPID Key - Get this from Firebase Console > Project Settings > Cloud Messaging > Web Push certificates
const VAPID_KEY = 'YOUR_VAPID_KEY_HERE';

// State
let currentToken = null;
const receivedNotifications = [];
let currentHistoryPage = 1;
const HISTORY_PAGE_SIZE = 20;

// DOM Elements
const permissionStatusEl = document.getElementById('permissionStatus');
const requestPermissionBtn = document.getElementById('requestPermissionBtn');
const getTokenBtn = document.getElementById('getTokenBtn');
const fcmTokenInput = document.getElementById('fcmToken');
const copyTokenBtn = document.getElementById('copyTokenBtn');
const registerForm = document.getElementById('registerForm');
const registerStatus = document.getElementById('registerStatus');
const sendNotificationForm = document.getElementById('sendNotificationForm');
const sendWithDataBtn = document.getElementById('sendWithDataBtn');
const sendStatus = document.getElementById('sendStatus');
const notificationsList = document.getElementById('notificationsList');
const clearNotificationsBtn = document.getElementById('clearNotificationsBtn');

// Schedule notification elements
const scheduleNotificationForm = document.getElementById('scheduleNotificationForm');
const scheduleStatus = document.getElementById('scheduleStatus');
const scheduledList = document.getElementById('scheduledList');
const refreshScheduledBtn = document.getElementById('refreshScheduledBtn');

// History elements
const notificationStats = document.getElementById('notificationStats');
const historyList = document.getElementById('historyList');
const refreshHistoryBtn = document.getElementById('refreshHistoryBtn');
const loadMoreHistoryBtn = document.getElementById('loadMoreHistoryBtn');

// Initialize app
document.addEventListener('DOMContentLoaded', () => {
    console.log('FCM Demo app loaded');
    updatePermissionStatus();
    setupEventListeners();
    registerServiceWorker();
    setupForegroundMessageHandler();
});

// Register service worker
async function registerServiceWorker() {
    if ('serviceWorker' in navigator) {
        try {
            const registration = await navigator.serviceWorker.register('/firebase-messaging-sw.js');
            console.log('Service Worker registered successfully:', registration);
        } catch (error) {
            console.error('Service Worker registration failed:', error);
        }
    }
}

// Setup event listeners
function setupEventListeners() {
    requestPermissionBtn.addEventListener('click', requestNotificationPermission);
    getTokenBtn.addEventListener('click', getFCMToken);
    copyTokenBtn.addEventListener('click', copyTokenToClipboard);
    registerForm.addEventListener('submit', handleTokenRegistration);
    sendNotificationForm.addEventListener('submit', handleSendNotification);
    sendWithDataBtn.addEventListener('click', handleSendNotificationWithData);
    clearNotificationsBtn.addEventListener('click', clearNotifications);

    // Scheduling event listeners
    scheduleNotificationForm.addEventListener('submit', handleScheduleNotification);
    refreshScheduledBtn.addEventListener('click', loadScheduledNotifications);

    // History event listeners
    refreshHistoryBtn.addEventListener('click', loadNotificationHistory);
    loadMoreHistoryBtn.addEventListener('click', loadMoreHistory);
}

// Update permission status display
function updatePermissionStatus() {
    const permission = Notification.permission;
    permissionStatusEl.textContent = permission;
    permissionStatusEl.className = `status ${permission}`;

    if (permission === 'granted') {
        requestPermissionBtn.disabled = true;
        requestPermissionBtn.textContent = 'Permission Granted ✓';
    } else if (permission === 'denied') {
        requestPermissionBtn.textContent = 'Permission Denied';
        requestPermissionBtn.disabled = true;
    }
}

// Request notification permission
async function requestNotificationPermission() {
    try {
        const permission = await Notification.requestPermission();
        console.log('Notification permission:', permission);
        updatePermissionStatus();

        if (permission === 'granted') {
            showMessage(registerStatus, 'Permission granted! You can now get your FCM token.', 'success');
        } else {
            showMessage(registerStatus, 'Permission denied. Please enable notifications in your browser settings.', 'error');
        }
    } catch (error) {
        console.error('Error requesting permission:', error);
        showMessage(registerStatus, `Error: ${error.message}`, 'error');
    }
}

// Get FCM token
async function getFCMToken() {
    if (Notification.permission !== 'granted') {
        showMessage(registerStatus, 'Please request notification permission first.', 'error');
        return;
    }

    try {
        getTokenBtn.disabled = true;
        getTokenBtn.textContent = 'Getting token...';

        currentToken = await getToken(messaging, { vapidKey: VAPID_KEY });

        if (currentToken) {
            fcmTokenInput.value = currentToken;
            console.log('FCM Token:', currentToken);
            showMessage(registerStatus, 'Token generated successfully! You can now register it with the backend.', 'success');
        } else {
            showMessage(registerStatus, 'Failed to generate token. Please check your Firebase configuration.', 'error');
        }
    } catch (error) {
        console.error('Error getting token:', error);
        showMessage(registerStatus, `Error: ${error.message}. Make sure you've configured Firebase correctly.`, 'error');
    } finally {
        getTokenBtn.disabled = false;
        getTokenBtn.textContent = 'Get Token';
    }
}

// Copy token to clipboard
function copyTokenToClipboard() {
    if (!currentToken) {
        showMessage(registerStatus, 'No token to copy. Please get a token first.', 'error');
        return;
    }

    fcmTokenInput.select();
    document.execCommand('copy');
    showMessage(registerStatus, 'Token copied to clipboard!', 'success');
}

// Handle token registration
async function handleTokenRegistration(e) {
    e.preventDefault();

    if (!currentToken) {
        showMessage(registerStatus, 'Please get your FCM token first.', 'error');
        return;
    }

    const userId = document.getElementById('userId').value;
    const deviceId = document.getElementById('deviceId').value;

    if (!userId || !deviceId) {
        showMessage(registerStatus, 'Please fill in User ID and Device ID.', 'error');
        return;
    }

    try {
        const tokenData = {
            userId,
            token: currentToken,
            deviceId,
            platform: 'Web'
        };

        const response = await registerDeviceToken(tokenData);
        console.log('Token registered:', response);
        showMessage(registerStatus, `✓ Token registered successfully! TokenId: ${response.tokenId}`, 'success');
    } catch (error) {
        console.error('Registration error:', error);
        showMessage(registerStatus, `Error: ${error.message}`, 'error');
    }
}

// Handle send notification
async function handleSendNotification(e) {
    e.preventDefault();

    if (!currentToken) {
        showMessage(sendStatus, 'Please get your FCM token first.', 'error');
        return;
    }

    const title = document.getElementById('notifTitle').value;
    const body = document.getElementById('notifBody').value;

    if (!title || !body) {
        showMessage(sendStatus, 'Please fill in notification title and body.', 'error');
        return;
    }

    try {
        const notificationData = {
            deviceToken: currentToken,
            title,
            body
        };

        const response = await sendNotification(notificationData);
        console.log('Notification sent:', response);

        if (response.success) {
            showMessage(sendStatus, `✓ Notification sent successfully! MessageId: ${response.messageId}`, 'success');
        } else {
            showMessage(sendStatus, `Error: ${response.error}`, 'error');
        }
    } catch (error) {
        console.error('Send notification error:', error);
        showMessage(sendStatus, `Error: ${error.message}`, 'error');
    }
}

// Handle send notification with data
async function handleSendNotificationWithData() {
    if (!currentToken) {
        showMessage(sendStatus, 'Please get your FCM token first.', 'error');
        return;
    }

    const title = document.getElementById('notifTitle').value;
    const body = document.getElementById('notifBody').value;

    if (!title || !body) {
        showMessage(sendStatus, 'Please fill in notification title and body.', 'error');
        return;
    }

    try {
        const notificationData = {
            deviceToken: currentToken,
            title,
            body,
            data: {
                action: 'view',
                url: 'https://firebase.google.com/docs/cloud-messaging',
                timestamp: new Date().toISOString(),
                customField: 'Example data payload'
            }
        };

        const response = await sendNotificationWithData(notificationData);
        console.log('Notification with data sent:', response);

        if (response.success) {
            showMessage(sendStatus, `✓ Notification with data sent! MessageId: ${response.messageId}`, 'success');
        } else {
            showMessage(sendStatus, `Error: ${response.error}`, 'error');
        }
    } catch (error) {
        console.error('Send notification with data error:', error);
        showMessage(sendStatus, `Error: ${error.message}`, 'error');
    }
}

// Setup foreground message handler
function setupForegroundMessageHandler() {
    onMessage(messaging, (payload) => {
        console.log('Foreground message received:', payload);

        // Add to received notifications list
        const notification = {
            title: payload.notification?.title || 'Notification',
            body: payload.notification?.body || '',
            data: payload.data || {},
            timestamp: new Date().toLocaleTimeString(),
            type: 'foreground'
        };

        receivedNotifications.unshift(notification);
        displayNotifications();

        // Show browser notification
        if (Notification.permission === 'granted') {
            new Notification(notification.title, {
                body: notification.body,
                icon: '/firebase-logo.png'
            });
        }
    });
}

// Display notifications in the list
function displayNotifications() {
    if (receivedNotifications.length === 0) {
        notificationsList.innerHTML = '<p class="empty-state">No notifications received yet.</p>';
        return;
    }

    notificationsList.innerHTML = receivedNotifications.map((notif, index) => `
        <div class="notification-item ${notif.type}">
            <div class="title">${notif.title}</div>
            <div class="body">${notif.body}</div>
            ${Object.keys(notif.data).length > 0 ? `
                <div class="data">
                    <strong>Data:</strong> ${JSON.stringify(notif.data, null, 2)}
                </div>
            ` : ''}
            <div class="meta">
                <span>${notif.timestamp}</span>
                <span class="badge">${notif.type}</span>
            </div>
        </div>
    `).join('');
}

// Clear notifications
function clearNotifications() {
    receivedNotifications.length = 0;
    displayNotifications();
}

// Show message helper
function showMessage(element, message, type) {
    element.textContent = message;
    element.className = `message ${type}`;
    element.style.display = 'block';

    // Auto-hide after 5 seconds
    setTimeout(() => {
        element.style.display = 'none';
    }, 5000);
}

// ============================================
// Scheduling Functions
// ============================================

// Handle schedule notification form submission
async function handleScheduleNotification(e) {
    e.preventDefault();

    const userId = document.getElementById('userId').value;
    const title = document.getElementById('scheduleTitle').value;
    const body = document.getElementById('scheduleBody').value;
    const scheduledFor = document.getElementById('scheduleTime').value;

    if (!currentToken) {
        showMessage(scheduleStatus, 'Please get your FCM token first', 'error');
        return;
    }

    if (!userId) {
        showMessage(scheduleStatus, 'Please register your token first (enter User ID in section 3)', 'error');
        return;
    }

    if (!scheduledFor) {
        showMessage(scheduleStatus, 'Please select a date and time', 'error');
        return;
    }

    try {
        const scheduleData = {
            userId: userId,
            deviceToken: currentToken,
            title: title,
            body: body,
            scheduledFor: new Date(scheduledFor).toISOString()
        };

        const response = await scheduleNotification(scheduleData);
        console.log('Notification scheduled:', response);

        showMessage(scheduleStatus, `Notification scheduled successfully! ID: ${response.id}`, 'success');

        // Reload scheduled notifications list
        await loadScheduledNotifications();

        // Clear form
        document.getElementById('scheduleTime').value = '';
    } catch (error) {
        console.error('Error scheduling notification:', error);
        showMessage(scheduleStatus, `Error: ${error.message}`, 'error');
    }
}

// Load scheduled notifications
async function loadScheduledNotifications() {
    const userId = document.getElementById('userId').value;

    if (!userId) {
        scheduledList.innerHTML = '<p class="empty-state">Please register first (enter User ID in section 3)</p>';
        return;
    }

    try {
        const notifications = await getUserScheduledNotifications(userId);
        displayScheduledNotifications(notifications);
    } catch (error) {
        console.error('Error loading scheduled notifications:', error);
        scheduledList.innerHTML = `<p class="error">Error loading scheduled notifications: ${error.message}</p>`;
    }
}

// Display scheduled notifications
function displayScheduledNotifications(notifications) {
    if (notifications.length === 0) {
        scheduledList.innerHTML = '<p class="empty-state">No scheduled notifications.</p>';
        return;
    }

    scheduledList.innerHTML = notifications.map(notif => {
        const scheduledDate = new Date(notif.scheduledFor).toLocaleString();
        const statusClass = notif.status.toLowerCase();

        return `
            <div class="scheduled-item status-${statusClass}">
                <div class="scheduled-header">
                    <span class="scheduled-title">${notif.title}</span>
                    <span class="badge badge-${statusClass}">${notif.status}</span>
                </div>
                <div class="scheduled-body">${notif.body}</div>
                <div class="scheduled-meta">
                    <span>📅 ${scheduledDate}</span>
                    <span>ID: ${notif.id}</span>
                </div>
                ${notif.status === 'Pending' ? `
                    <button class="btn btn-sm btn-danger" onclick="cancelScheduled(${notif.id})">Cancel</button>
                ` : ''}
                ${notif.errorMessage ? `<div class="error-msg">Error: ${notif.errorMessage}</div>` : ''}
            </div>
        `;
    }).join('');
}

// Cancel scheduled notification (global function for onclick)
window.cancelScheduled = async function(id) {
    if (!confirm('Are you sure you want to cancel this scheduled notification?')) {
        return;
    }

    try {
        await cancelScheduledNotification(id);
        showMessage(scheduleStatus, 'Scheduled notification cancelled successfully', 'success');
        await loadScheduledNotifications();
    } catch (error) {
        console.error('Error cancelling scheduled notification:', error);
        showMessage(scheduleStatus, `Error: ${error.message}`, 'error');
    }
};

// ============================================
// History Functions
// ============================================

// Load notification history
async function loadNotificationHistory() {
    const userId = document.getElementById('userId').value;

    if (!userId) {
        historyList.innerHTML = '<p class="empty-state">Please register first (enter User ID in section 3)</p>';
        return;
    }

    currentHistoryPage = 1;

    try {
        // Load stats
        const stats = await getNotificationStats(userId);
        displayNotificationStats(stats);

        // Load history
        const history = await getUserNotificationHistory(userId, HISTORY_PAGE_SIZE, currentHistoryPage);
        displayNotificationHistory(history, true);
    } catch (error) {
        console.error('Error loading notification history:', error);
        historyList.innerHTML = `<p class="error">Error loading history: ${error.message}</p>`;
    }
}

// Load more history (pagination)
async function loadMoreHistory() {
    const userId = document.getElementById('userId').value;

    if (!userId) {
        return;
    }

    currentHistoryPage++;

    try {
        const history = await getUserNotificationHistory(userId, HISTORY_PAGE_SIZE, currentHistoryPage);
        displayNotificationHistory(history, false);
    } catch (error) {
        console.error('Error loading more history:', error);
        currentHistoryPage--; // Revert page number on error
    }
}

// Display notification statistics
function displayNotificationStats(stats) {
    document.getElementById('statTotalSent').textContent = stats.totalSent;
    document.getElementById('statSuccessful').textContent = stats.successCount;
    document.getElementById('statFailed').textContent = stats.failedCount;
    document.getElementById('statSuccessRate').textContent = `${stats.successRate.toFixed(1)}%`;
}

// Display notification history
function displayNotificationHistory(history, replace = true) {
    if (history.length === 0 && replace) {
        historyList.innerHTML = '<p class="empty-state">No notification history available.</p>';
        loadMoreHistoryBtn.style.display = 'none';
        return;
    }

    if (history.length < HISTORY_PAGE_SIZE) {
        loadMoreHistoryBtn.style.display = 'none';
    } else {
        loadMoreHistoryBtn.style.display = 'inline-block';
    }

    const historyHTML = history.map(log => {
        const sentDate = new Date(log.sentAt).toLocaleString();
        const statusClass = log.success ? 'success' : 'failed';
        const statusIcon = log.success ? '✅' : '❌';

        return `
            <div class="history-item status-${statusClass}">
                <div class="history-header">
                    <span class="history-title">${statusIcon} ${log.title}</span>
                    <span class="badge badge-${statusClass}">${log.success ? 'Success' : 'Failed'}</span>
                </div>
                <div class="history-body">${log.body}</div>
                <div class="history-meta">
                    <span>📅 ${sentDate}</span>
                    <span>Platform: ${log.platform}</span>
                    ${log.firebaseMessageId ? `<span>Message ID: ${log.firebaseMessageId.substring(0, 20)}...</span>` : ''}
                </div>
                ${log.data ? `
                    <div class="history-data">
                        <strong>Data:</strong> ${JSON.stringify(log.data, null, 2)}
                    </div>
                ` : ''}
                ${log.errorMessage ? `<div class="error-msg">Error: ${log.errorMessage}</div>` : ''}
            </div>
        `;
    }).join('');

    if (replace) {
        historyList.innerHTML = historyHTML;
    } else {
        historyList.innerHTML += historyHTML;
    }
}
