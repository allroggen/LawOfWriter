// ─────────────────────────────────────────────────────────────────────────────
// Network status – online/offline detection for ConnectivityService
// ─────────────────────────────────────────────────────────────────────────────

window.networkStatus = {
    _dotNetRef: null,
    _onlineHandler: null,
    _offlineHandler: null,

    initialize: function (dotNetRef) {
        this._dotNetRef = dotNetRef;

        this._onlineHandler = () => {
            dotNetRef.invokeMethodAsync('OnNetworkStatusChanged', true)
                .catch(err => console.warn('[networkStatus] online notify failed:', err));
        };
        this._offlineHandler = () => {
            dotNetRef.invokeMethodAsync('OnNetworkStatusChanged', false)
                .catch(err => console.warn('[networkStatus] offline notify failed:', err));
        };

        window.addEventListener('online', this._onlineHandler);
        window.addEventListener('offline', this._offlineHandler);

        return navigator.onLine;
    },

    dispose: function () {
        if (this._onlineHandler) window.removeEventListener('online', this._onlineHandler);
        if (this._offlineHandler) window.removeEventListener('offline', this._offlineHandler);
        this._onlineHandler = null;
        this._offlineHandler = null;
        this._dotNetRef = null;
    },

    isOnline: function () {
        return navigator.onLine;
    }
};
const DB_NAME = 'LawOfWriterDb';
const DB_VERSION = 2;
const STORE_GAME_DAYS = 'gameDays';
const STORE_GAME_ACTIONS = 'gameDayActions';
const STORE_DRINK_NOTES = 'drinkNotes';

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

            // v2: Drink notes store (handwritten notes on canvas)
            if (!db.objectStoreNames.contains(STORE_DRINK_NOTES)) {
                const store = db.createObjectStore(STORE_DRINK_NOTES, { keyPath: 'id', autoIncrement: true });
                store.createIndex('gameId', 'gameId', { unique: false });
                store.createIndex('createdAt', 'createdAt', { unique: false });
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
            const req = tx.objectStore(STORE_GAME_ACTIONS).getAll();
            req.onsuccess = () => resolve(JSON.stringify(req.result.filter(r => !r.isSynced)));
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
                    const putReq = store.put(record);
                    putReq.onerror = () => reject(putReq.error);
                }
            };
            getReq.onerror = () => reject(getReq.error);
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
    },

    // ---------- DrinkNotes (handwritten canvas notes) ----------

    async saveDrinkNote(noteJson) {
        const db = await openDb();
        const note = JSON.parse(noteJson);
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_DRINK_NOTES, 'readwrite');
            const req = tx.objectStore(STORE_DRINK_NOTES).put(note);
            req.onsuccess = () => resolve(req.result); // returns the auto-generated id
            tx.onerror = () => reject(tx.error);
        });
    },

    async getDrinkNote(id) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_DRINK_NOTES, 'readonly');
            const req = tx.objectStore(STORE_DRINK_NOTES).get(id);
            req.onsuccess = () => resolve(req.result ? JSON.stringify(req.result) : null);
            req.onerror = () => reject(req.error);
        });
    },

    async getDrinkNotesByGameId(gameId) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_DRINK_NOTES, 'readonly');
            const index = tx.objectStore(STORE_DRINK_NOTES).index('gameId');
            const req = index.getAll(gameId);
            req.onsuccess = () => resolve(JSON.stringify(req.result));
            req.onerror = () => reject(req.error);
        });
    },

    async getAllDrinkNotes() {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_DRINK_NOTES, 'readonly');
            const req = tx.objectStore(STORE_DRINK_NOTES).getAll();
            req.onsuccess = () => resolve(JSON.stringify(req.result));
            req.onerror = () => reject(req.error);
        });
    },

    async deleteDrinkNote(id) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_DRINK_NOTES, 'readwrite');
            tx.objectStore(STORE_DRINK_NOTES).delete(id);
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    },

    async clearDrinkNotes() {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE_DRINK_NOTES, 'readwrite');
            tx.objectStore(STORE_DRINK_NOTES).clear();
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    }
};

