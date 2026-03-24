// Token de Microsoft Graph via MSAL interno de Blazor WASM.
// window.AuthenticationService.instance._msalApplication = PublicClientApplication
// MSAL está configurado con cacheLocation: "localStorage" para persistir tokens entre recargas.

const GRAPH_SCOPES = [
    "https://graph.microsoft.com/Files.Read",
    "https://graph.microsoft.com/Files.ReadWrite"
];

const LS_TOKEN_KEY = "novahub_graph_token";
const LS_TOKEN_EXP  = "novahub_graph_token_exp";

async function waitForMsal(maxMs = 8000) {
    const interval = 100;
    const maxTries = maxMs / interval;
    for (let i = 0; i < maxTries; i++) {
        const msal = window.AuthenticationService?.instance?._msalApplication;
        if (msal?.acquireTokenSilent) return msal;
        await new Promise(r => setTimeout(r, interval));
    }
    return null;
}

function getAccount(msal) {
    return msal.getActiveAccount() || msal.getAllAccounts()?.[0] || null;
}

function requiresInteraction(errorCode) {
    return [
        "interaction_required",
        "login_required",
        "consent_required",
        "invalid_grant",
        "no_account_in_silent_request"
    ].includes(errorCode);
}

// Guarda token en localStorage con expiración
function saveToken(token, expiresInSeconds = 3500) {
    const exp = Date.now() + expiresInSeconds * 1000;
    localStorage.setItem(LS_TOKEN_KEY, token);
    localStorage.setItem(LS_TOKEN_EXP, String(exp));
}

// Lee token del localStorage si no expiró (con 60s de margen)
function loadCachedToken() {
    const token = localStorage.getItem(LS_TOKEN_KEY);
    const exp   = parseInt(localStorage.getItem(LS_TOKEN_EXP) || "0", 10);
    if (token && exp > Date.now() + 60_000) return token;
    return null;
}

function clearCachedToken() {
    localStorage.removeItem(LS_TOKEN_KEY);
    localStorage.removeItem(LS_TOKEN_EXP);
}

export async function obtenerTokenGraph() {
    try {
        // 1. Intentar desde caché local primero (evita llamada a MSAL si ya tenemos token válido)
        const cached = loadCachedToken();
        if (cached) {
            return { token: cached, error: null, needsConsent: false };
        }

        const msal = await waitForMsal();
        if (!msal) {
            return { token: null, error: "Servicio de autenticación no disponible.", needsConsent: false };
        }

        const account = getAccount(msal);
        if (!account) {
            return { token: null, error: null, needsConsent: true };
        }

        try {
            const result = await msal.acquireTokenSilent({
                scopes: GRAPH_SCOPES,
                account,
                forceRefresh: false
            });
            if (result?.accessToken) {
                // Calcular segundos hasta expiración
                const expSecs = result.expiresOn
                    ? Math.floor((result.expiresOn.getTime() - Date.now()) / 1000)
                    : 3500;
                saveToken(result.accessToken, expSecs);
                return { token: result.accessToken, error: null, needsConsent: false };
            }
        } catch (err) {
            const code = err?.errorCode || "";
            if (requiresInteraction(code)) {
                return { token: null, error: null, needsConsent: true };
            }
            return { token: null, error: "Error al obtener token: " + (err?.message || code), needsConsent: false };
        }

        return { token: null, error: null, needsConsent: true };

    } catch (e) {
        return { token: null, error: "Error inesperado: " + (e?.message || e), needsConsent: false };
    }
}

export async function obtenerTokenGraphConPopup() {
    try {
        const msal = await waitForMsal();
        if (!msal) {
            return { token: null, error: "Servicio de autenticación no disponible.", needsConsent: false };
        }

        const account = getAccount(msal);

        const popupRequest = {
            scopes: GRAPH_SCOPES,
            ...(account ? { account } : {})
        };

        try {
            const result = await msal.acquireTokenPopup(popupRequest);
            if (result?.accessToken) {
                const expSecs = result.expiresOn
                    ? Math.floor((result.expiresOn.getTime() - Date.now()) / 1000)
                    : 3500;
                saveToken(result.accessToken, expSecs);
                return { token: result.accessToken, error: null, needsConsent: false };
            }
            return { token: null, error: "No se obtuvo token.", needsConsent: true };
        } catch (popupErr) {
            const msg = popupErr?.message || "";
            const code = popupErr?.errorCode || "";
            if (code === "user_cancelled" || msg.includes("user_cancelled")) {
                return { token: null, error: "Cancelaste la autorización.", needsConsent: true };
            }
            if (code === "popup_window_error") {
                return { token: null, error: "El navegador bloqueó el popup. Permite popups para este sitio.", needsConsent: true };
            }
            return { token: null, error: "Error: " + (msg || code), needsConsent: true };
        }

    } catch (e) {
        return { token: null, error: "Error inesperado: " + (e?.message || e), needsConsent: false };
    }
}
