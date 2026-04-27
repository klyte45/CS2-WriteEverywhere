import engine from "cohtml/cohtml";

export const translate = function (key: string, fallback?: string) {
    const fullKey = `K45::WE.vuio[${key}]`;
    const tr = engine.translate(fullKey);
    if (tr === fullKey) {
        if (fallback !== undefined) {
            return fallback;
        }
        (window as any).K45_MISSING_I18N ??= new Set<string>();
        ((window as any).K45_MISSING_I18N as Set<string>).add(fullKey);
    }
    return tr;
}
export const translate_formulaCategory = function (dll: string, category?: string, fallback?: string) {
    const fullKey = `K45::WE.formulaCategory[${dll.replace(".", "_")}${category ? "." + category : ""}]`;
    const tr = engine.translate(fullKey);
    if (tr === fullKey) {
        (window as any).WE_MISSING_I18N ??= new Set<string>();
        ((window as any).WE_MISSING_I18N as Set<string>).add(fullKey);
        if (fallback !== undefined) {
            return fallback;
        }
    }
    return tr;
}

