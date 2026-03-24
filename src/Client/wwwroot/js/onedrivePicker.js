// OneDrive/SharePoint File Picker v8 — Microsoft 365 organizacional
// El token se obtiene desde Blazor (C#) y se pasa al picker via postMessage

let pickerWindow = null;
let messageHandler = null;

const BASE_URL = "https://labnovaderma.sharepoint.com";

export async function abrirPicker(dotnetRef) {
    if (messageHandler) {
        window.removeEventListener("message", messageHandler);
        messageHandler = null;
    }

    const params = {
        sdk: "8.0",
        entry: {
            sharePoint: { byPath: { web: BASE_URL } }
        },
        authentication: {},
        messaging: {
            origin: window.location.origin,
            channelId: "27"
        },
        typesAndSources: {
            mode: "files",
            pivots: { oneDrive: true, recent: true, sharedLibraries: true }
        },
        selection: { mode: "single" },
        search: { enabled: true }
    };

    const pickerUrl = `${BASE_URL}/_layouts/15/FilePicker.aspx?filePicker=${encodeURIComponent(JSON.stringify(params))}`;
    pickerWindow = window.open(pickerUrl, "picker", "width=900,height=650,resizable=yes");

    messageHandler = async (event) => {
        if (!pickerWindow) return;
        if (event.origin !== BASE_URL) return;

        const message = event.data;
        if (typeof message !== "object" || !message.type) return;

        if (message.type === "initialize" && message.channelId === "27") {
            const port = event.ports[0];
            port.addEventListener("message", async (e) => {
                await handleMessage(e.data, port, dotnetRef);
            });
            port.start();
            port.postMessage({ type: "activate" });
        }
    };

    window.addEventListener("message", messageHandler);
}

async function handleMessage(message, port, dotnetRef) {
    if (message.type !== "command") return;

    port.postMessage({ type: "acknowledge", id: message.id });
    const command = message.data;

    switch (command.command) {
        case "authenticate": {
            // Pedir token a Blazor (C#) según el recurso solicitado
            let token = null;
            try {
                token = await dotnetRef.invokeMethodAsync("ObtenerToken", command.resource || BASE_URL);
            } catch (e) {
                console.error("Error obteniendo token:", e);
            }

            if (token) {
                port.postMessage({ type: "result", id: message.id, data: { result: "token", token } });
            } else {
                port.postMessage({ type: "result", id: message.id, data: { result: "error", error: { code: "tokenError" } } });
            }
            break;
        }

        case "pick": {
            const items = command.items;
            if (items?.length > 0) {
                const item = items[0];
                const webUrl = item.webUrl || "";
                const nombre = item.name || "";
                await dotnetRef.invokeMethodAsync("OnArchivoSeleccionado", webUrl, nombre);
            }
            port.postMessage({ type: "result", id: message.id, data: { result: "success" } });
            cerrar();
            break;
        }

        case "close":
            cerrar();
            break;
    }
}

function cerrar() {
    pickerWindow?.close();
    pickerWindow = null;
    if (messageHandler) {
        window.removeEventListener("message", messageHandler);
        messageHandler = null;
    }
}
