// binaryen_wrapper.c
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>
#include <stdint.h>
#include <binaryen-c.h>

#define EXPORT __attribute__((visibility("default")))

/* =========================
 * Small helpers (WAT parsing)
 * ========================= */

static const char* skip_ws(const char* p) {
    while (p && (*p == ' ' || *p == '\n' || *p == '\r' || *p == '\t')) p++;
    return p;
}

static const char* find_matching_paren(const char* p) {
    if (!p || *p != '(') return NULL;
    int depth = 0;
    while (*p) {
        if (*p == '(') depth++;
        else if (*p == ')') {
            depth--;
            if (depth == 0) return p; // matching ')'
        }
        p++;
    }
    return NULL;
}

// Extract first "(?.const VALUE)" inside [start, end] (end points to matching ')')
static char* extract_const_from_global_block(const char* start, const char* end) {
    if (!start || !end || end <= start) return NULL;

    const char* p = start;
    while (p < end) {
        // find next '('
        while (p < end && *p != '(') p++;
        if (p >= end) break;

        const char* q = skip_ws(p + 1);

        // read opcode token: e.g. "i32.const"
        const char* tok = q;
        while (q < end && *q &&
               *q != ' ' && *q != '\n' && *q != '\r' && *q != '\t' && *q != ')') {
            q++;
        }

        size_t len = (size_t)(q - tok);
        if (len >= 6 && memcmp(tok + (len - 6), ".const", 6) == 0) {
            // read value token after opcode
            const char* v = skip_ws(q);
            const char* ve = v;
            while (ve < end && *ve &&
                   *ve != ')' && *ve != ' ' && *ve != '\n' && *ve != '\r' && *ve != '\t') {
                ve++;
            }
            if (ve > v) {
                size_t vlen = (size_t)(ve - v);
                char* out = (char*)malloc(vlen + 1);
                if (!out) return NULL;
                memcpy(out, v, vlen);
                out[vlen] = 0;
                return out;
            }
        }

        p = p + 1;
    }

    return NULL;
}

/* =========================
 * Module loading / printing
 * ========================= */

/// Load a compiled WASM binary (.wasm) into a Binaryen module
EXPORT BinaryenModuleRef LoadWasmTextFile(const char* filename) {
    FILE* file = fopen(filename, "rb");
    if (!file) {
        fprintf(stderr, "Error: Cannot open file %s\n", filename);
        return NULL;
    }

    fseek(file, 0, SEEK_END);
    long fsize_long = ftell(file);
    if (fsize_long < 0) {
        fclose(file);
        fprintf(stderr, "Error: ftell failed for %s\n", filename);
        return NULL;
    }
    size_t fsize = (size_t)fsize_long;
    rewind(file);

    char* buffer = (char*)malloc(fsize);
    if (!buffer) {
        fprintf(stderr, "Error: Memory allocation failed.\n");
        fclose(file);
        return NULL;
    }

    size_t readn = fread(buffer, 1, fsize, file);
    fclose(file);
    if (readn != fsize) {
        free(buffer);
        fprintf(stderr, "Error: fread failed (read %zu of %zu)\n", readn, fsize);
        return NULL;
    }

    BinaryenSetDebugInfo(true); // pour garder les noms (debug names)

    BinaryenModuleRef module = BinaryenModuleRead(buffer, fsize);
    free(buffer);

    if (!module) {
        fprintf(stderr, "Error: BinaryenModuleRead failed for %s\n", filename);
        return NULL;
    }

    // Optionnel: afficher le module (debug)
    char* text = BinaryenModuleAllocateAndWriteText(module);
    if (text) {
        printf("%s\n", text);
        free(text);
    }

    return module;
}

/// Validate a Binaryen module
EXPORT bool ValidateModule(BinaryenModuleRef module) {
    if (!module) return false;
    return BinaryenModuleValidate(module);
}

/// Print the module as WAT (for debugging)
EXPORT void PrintModuleAST(BinaryenModuleRef module) {
    if (!module) {
        fprintf(stderr, "❌ PrintModuleAST: null module\n");
        return;
    }
    char* watText = BinaryenModuleAllocateAndWriteText(module);
    if (watText) {
        printf("\n===== AST WAT (from Binaryen) =====\n");
        printf("%s\n", watText);
        printf("=====================================\n\n");
        free(watText);
    } else {
        fprintf(stderr, "❌ Unable to obtain the textual AST of the module.\n");
    }
}

/* =========================
 * Function enumeration
 * ========================= */

EXPORT int GetFunctionCount(BinaryenModuleRef module) {
    if (!module) return 0;
    return BinaryenGetNumFunctions(module);
}

/// Returned pointer is owned by Binaryen; DO NOT free it.
EXPORT const char* GetFunctionNameByIndex(BinaryenModuleRef module, int index) {
    if (!module) return "";
    int n = BinaryenGetNumFunctions(module);
    if (index < 0 || index >= n) return "";
    BinaryenFunctionRef func = BinaryenGetFunctionByIndex(module, index);
    if (!func) return "";
    const char* name = BinaryenFunctionGetName(func);
    return name ? name : "";
}

/* =========================
 * Global enumeration
 * ========================= */

EXPORT int GetGlobalCount(BinaryenModuleRef module) {
    if (!module) return 0;
    return BinaryenGetNumGlobals(module);
}

/// Returned pointer is owned by Binaryen; DO NOT free it.
EXPORT const char* GetGlobalNameByIndex(BinaryenModuleRef module, int index) {
    if (!module) return "";
    int n = BinaryenGetNumGlobals(module);
    if (index < 0 || index >= n) return "";
    BinaryenGlobalRef g = BinaryenGetGlobalByIndex(module, index);
    if (!g) return "";
    const char* name = BinaryenGlobalGetName(g);
    return name ? name : "";
}

EXPORT int GetGlobalIsMutableByIndex(BinaryenModuleRef module, int index) {
    if (!module) return 0;
    int n = BinaryenGetNumGlobals(module);
    if (index < 0 || index >= n) return 0;
    BinaryenGlobalRef g = BinaryenGetGlobalByIndex(module, index);
    if (!g) return 0;
    return BinaryenGlobalIsMutable(g) ? 1 : 0;
}

/// Returned string is heap-allocated (via strdup); call FreeCString()
EXPORT const char* GetGlobalTypeByIndex(BinaryenModuleRef module, int index) {
    if (!module) return strdup("");

    int n = BinaryenGetNumGlobals(module);
    if (index < 0 || index >= n) return strdup("");

    BinaryenGlobalRef g = BinaryenGetGlobalByIndex(module, index);
    if (!g) return strdup("");

    BinaryenType t = BinaryenGlobalGetType(g);

    if (t == BinaryenTypeInt32()) return strdup("i32");
    if (t == BinaryenTypeInt64()) return strdup("i64");
    if (t == BinaryenTypeFloat32()) return strdup("f32");
    if (t == BinaryenTypeFloat64()) return strdup("f64");

    return strdup("unknown");
}

/// ✅ SAFE for old Binaryen: get init by parsing the WAT text (no getf64 crash)
EXPORT const char* GetGlobalInitConstByIndex(BinaryenModuleRef module, int index) {
    if (!module) return strdup("");

    // Dump module to text and locate the index-th "(global ...)"
    char* wat = BinaryenModuleAllocateAndWriteText(module);
    if (!wat) return strdup("");

    const char* p = wat;
    int gi = 0;
    const char* globalStart = NULL;

    while (*p) {
        const char* g = strstr(p, "(global");
        if (!g) break;
        if (gi == index) { globalStart = g; break; }
        gi++;
        p = g + 7;
    }

    if (!globalStart) {
        free(wat);
        return strdup("");
    }

    const char* globalEnd = find_matching_paren(globalStart);
    if (!globalEnd) {
        free(wat);
        return strdup("");
    }

    char* val = extract_const_from_global_block(globalStart, globalEnd);
    const char* ret = val ? strdup(val) : strdup("");

    if (val) free(val);
    free(wat);
    return ret;
}

/* =========================
 * Function body as WAT
 * ========================= */

EXPORT BinaryenExpressionRef GetFunctionBody(BinaryenModuleRef module, int index) {
    if (!module) return NULL;
    int n = BinaryenGetNumFunctions(module);
    if (index < 0 || index >= n) return NULL;
    BinaryenFunctionRef func = BinaryenGetFunctionByIndex(module, index);
    if (!func) return NULL;
    return BinaryenFunctionGetBody(func);
}

EXPORT int GetFunctionResultCount(BinaryenModuleRef module, int index) {
    if (!module) return 0;
    int n = BinaryenGetNumFunctions(module);
    if (index < 0 || index >= n) return 0;
    BinaryenFunctionRef f = BinaryenGetFunctionByIndex(module, index);
    if (!f) return 0;
    BinaryenType results = BinaryenFunctionGetResults(f);
    return BinaryenTypeArity(results);
}

EXPORT int GetFunctionParamCount(BinaryenModuleRef module, int index) {
    if (!module) return 0;
    int n = BinaryenGetNumFunctions(module);
    if (index < 0 || index >= n) return 0;
    BinaryenFunctionRef f = BinaryenGetFunctionByIndex(module, index);
    if (!f) return 0;
    BinaryenType params = BinaryenFunctionGetParams(f);
    return BinaryenTypeArity(params);
}

/// Returned C string is heap-allocated (via strdup); call FreeCString()
EXPORT const char* GetFunctionBodyText(BinaryenModuleRef module, int index) {
    if (!module) return strdup("");

    int n = BinaryenGetNumFunctions(module);
    if (index < 0 || index >= n) return strdup("");

    BinaryenFunctionRef func = BinaryenGetFunctionByIndex(module, index);
    if (!func) return strdup("");

    BinaryenExpressionRef body = BinaryenFunctionGetBody(func);
    if (!body) return strdup("");

    BinaryenType params  = BinaryenFunctionGetParams(func);
    BinaryenType results = BinaryenFunctionGetResults(func);

    BinaryenModuleRef tempMod = BinaryenModuleCreate();
    if (!tempMod) return strdup("");

    BinaryenExpressionRef copied = BinaryenExpressionCopy(body, tempMod);

    BinaryenAddFunction(
        tempMod,
        "temp",
        params,
        results,
        NULL, 0,
        copied
    );

    char* wat = BinaryenModuleAllocateAndWriteText(tempMod);
    const char* result = wat ? strdup(wat) : strdup("");
    if (wat) free(wat);

    BinaryenModuleDispose(tempMod);
    return result;
}

EXPORT void FreeCString(const char* s) {
    if (s) free((void*)s);
}
