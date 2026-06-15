import { config } from "@/lib/config";

// Runs before first paint to set the .dark class from storage or system, avoiding a flash.
export const themeScript = `(function(){try{var k='${config.themeStorageKey}';var s=localStorage.getItem(k);var m=window.matchMedia('(prefers-color-scheme: dark)').matches;var d=s==='dark'||((s===null||s==='system')&&m);document.documentElement.classList.toggle('dark',d);}catch(e){}})();`;
