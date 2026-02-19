// IndexedDB helper for LawOfWriter offline storage
const DB_NAME = 'LawOfWriterDb';
const DB_VERSION = 1;
const STORE_GAME_DAYS = 'gameDays';
const STORE_GAME_ACTIONS = 'gameDayActions';

function openDb() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);

        request.onupgradeneeded = (event) => {
            const db = event.target.result;

            if (!db.objectStoreNames.contains(STORE_GAME_DAYS)) {
                db.createObjectStore(STORE_GAME_DAYS, { keyPath: 'id' });
            }

            if (!db.objectStoreNames.contains(STORE_GAME_ACTIONS)) {
                const store = db.createObjectStore(STORE_GAME_ACTIONS, { keyPath: 'id' });
                store.createIndex('gameId', 'gameId', { unique: false });
                store.createIndex('isSynced', 'isSynced', { unique: false });
            }
        };

        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

// ---------- GameDay ----------

window.localDb = {

    async saveGameDay(gameDayJson) {
        const db = await openDb();
        const gameDay = JSON.parse(gameDayJson);
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_GAME_DAYS, 'readwrite');
            tx.objectStore(STORE_GAME_DAYS).put(gameDay);
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    },

    async getGameDay(id) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_GAME_DAYS, 'readonly');
            const req = tx.objectStore(STORE_GAME_DAYS).get(id);
            req.onsuccess = () => resolve(req.result ? JSON.stringify(req.result) : null);
            req.onerror = () => reject(req.error);
        });
    },

    async getAllGameDays() {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_GAME_DAYS, 'readonly');
            const req = tx.objectStore(STORE_GAME_DAYS).getAll();
            req.onsuccess = () => resolve(JSON.stringify(req.result));
            req.onerror = () => reject(req.error);
        });
    },

    async deleteGameDay(id) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_GAME_DAYS, 'readwrite');
            tx.objectStore(STORE_GAME_DAYS).delete(id);
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    },

    // ---------- GameDayAction ----------

    async saveGameDayAction(actionJson) {
        const db = await openDb();
        const action = JSON.parse(actionJson);
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_GAME_ACTIONS, 'readwrite');
            tx.objectStore(STORE_GAME_ACTIONS).put(action);
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    },

    async getGameDayAction(id) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_GAME_ACTIONS, 'readonly');
            const req = tx.objectStore(STORE_GAME_ACTIONS).get(id);
            req.onsuccess = () => resolve(req.result ? JSON.stringify(req.result) : null);
            req.onerror = () => reject(req.error);
        });
    },

    async getActionsByGameId(gameId) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_GAME_ACTIONS, 'readonly');
            const index = tx.objectStore(STORE_GAME_ACTIONS).index('gameId');
            const req = index.getAll(gameId);
            req.onsuccess = () => resolve(JSON.stringify(req.result));
            req.onerror = () => reject(req.error);
        });
    },

    async getUnsyncedActions() {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_GAME_ACTIONS, 'readonly');
            const index = tx.objectStore(STORE_GAME_ACTIONS).index('isSynced');
            const req = index.getAll(false);
            req.onsuccess = () => resolve(JSON.stringify(req.result));
            req.onerror = () => reject(req.error);
        });
    },

    async markActionAsSynced(id) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_GAME_ACTIONS, 'readwrite');
            const store = tx.objectStore(STORE_GAME_ACTIONS);
            const getReq = store.get(id);
            getReq.onsuccess = () => {
                const record = getReq.result;
                if (record) {
                    record.isSynced = true;
                    store.put(record);
                }
            };
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    },

    async markAllActionsAsSynced(gameId) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_GAME_ACTIONS, 'readwrite');
            const store = tx.objectStore(STORE_GAME_ACTIONS);
            const index = store.index('gameId');
            const req = index.getAll(gameId);
            req.onsuccess = () => {
                for (const record of req.result) {
                    record.isSynced = true;
                    store.put(record);
                }
            };
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    },

    async deleteGameDayAction(id) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_GAME_ACTIONS, 'readwrite');
            tx.objectStore(STORE_GAME_ACTIONS).delete(id);
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    },

    async clearAll() {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction([STORE_GAME_DAYS, STORE_GAME_ACTIONS], 'readwrite');
            tx.objectStore(STORE_GAME_DAYS).clear();
            tx.objectStore(STORE_GAME_ACTIONS).clear();
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    }
};

