// API Base URL
const API_BASE_URL = 'https://localhost:7001/api';

/**
 * Register device token with backend API
 * @param {Object} tokenData - Token registration data
 * @returns {Promise<Object>} Registration response
 */
export async function registerDeviceToken(tokenData) {
    try {
        const response = await fetch(`${API_BASE_URL}/devicetokens/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(tokenData)
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Failed to register token');
        }

        return await response.json();
    } catch (error) {
        console.error('Error registering device token:', error);
        throw error;
    }
}

/**
 * Send a simple notification
 * @param {Object} notificationData - Notification details
 * @returns {Promise<Object>} Send response
 */
export async function sendNotification(notificationData) {
    try {
        const response = await fetch(`${API_BASE_URL}/notifications/send`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(notificationData)
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Failed to send notification');
        }

        return await response.json();
    } catch (error) {
        console.error('Error sending notification:', error);
        throw error;
    }
}

/**
 * Send a notification with custom data payload
 * @param {Object} notificationData - Notification details with data
 * @returns {Promise<Object>} Send response
 */
export async function sendNotificationWithData(notificationData) {
    try {
        const response = await fetch(`${API_BASE_URL}/notifications/send-with-data`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(notificationData)
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Failed to send notification with data');
        }

        return await response.json();
    } catch (error) {
        console.error('Error sending notification with data:', error);
        throw error;
    }
}

/**
 * Unregister device token
 * @param {string} deviceId - Device identifier
 * @returns {Promise<Object>} Unregister response
 */
export async function unregisterDeviceToken(deviceId) {
    try {
        const response = await fetch(`${API_BASE_URL}/devicetokens/${deviceId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Failed to unregister token');
        }

        return await response.json();
    } catch (error) {
        console.error('Error unregister device token:', error);
        throw error;
    }
}

/**
 * Schedule a notification for future delivery
 * @param {Object} scheduleData - Scheduled notification details
 * @returns {Promise<Object>} Schedule response
 */
export async function scheduleNotification(scheduleData) {
    try {
        const response = await fetch(`${API_BASE_URL}/scheduling/schedule`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(scheduleData)
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to schedule notification');
        }

        return await response.json();
    } catch (error) {
        console.error('Error scheduling notification:', error);
        throw error;
    }
}

/**
 * Get scheduled notifications for a user
 * @param {string} userId - User identifier
 * @param {string} status - Optional status filter (Pending, Sent, Failed, Cancelled)
 * @returns {Promise<Array>} List of scheduled notifications
 */
export async function getUserScheduledNotifications(userId, status = null) {
    try {
        let url = `${API_BASE_URL}/scheduling/user/${userId}`;
        if (status) {
            url += `?status=${status}`;
        }

        const response = await fetch(url);

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to get scheduled notifications');
        }

        return await response.json();
    } catch (error) {
        console.error('Error getting scheduled notifications:', error);
        throw error;
    }
}

/**
 * Cancel a scheduled notification
 * @param {number} id - Scheduled notification ID
 * @returns {Promise<Object>} Cancel response
 */
export async function cancelScheduledNotification(id) {
    try {
        const response = await fetch(`${API_BASE_URL}/scheduling/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to cancel scheduled notification');
        }

        return await response.json();
    } catch (error) {
        console.error('Error cancelling scheduled notification:', error);
        throw error;
    }
}

/**
 * Get notification history for a user
 * @param {string} userId - User identifier
 * @param {number} pageSize - Number of items per page
 * @param {number} pageNumber - Page number (1-based)
 * @returns {Promise<Array>} List of notification logs
 */
export async function getUserNotificationHistory(userId, pageSize = 20, pageNumber = 1) {
    try {
        const url = `${API_BASE_URL}/history/user/${userId}?pageSize=${pageSize}&pageNumber=${pageNumber}`;

        const response = await fetch(url);

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to get notification history');
        }

        return await response.json();
    } catch (error) {
        console.error('Error getting notification history:', error);
        throw error;
    }
}

/**
 * Get notification statistics for a user
 * @param {string} userId - User identifier
 * @returns {Promise<Object>} Notification statistics
 */
export async function getNotificationStats(userId) {
    try {
        const response = await fetch(`${API_BASE_URL}/history/stats/${userId}`);

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to get notification stats');
        }

        return await response.json();
    } catch (error) {
        console.error('Error getting notification stats:', error);
        throw error;
    }
}
