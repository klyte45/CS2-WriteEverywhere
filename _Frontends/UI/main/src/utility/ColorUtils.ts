import { ObjectTyped } from "object-typed";

export interface ColorPointerData {
    currentColorId: number
}

type _ColorStruct = { clr: Color01, lum: number, idx?: number }
export type Color01 = { a: number, r: number, g: number, b: number }


export class ColorUtils {
    static toColor01(x: string): Color01 {
        let colors = /#([0-9a-fA-F]{2})([0-9a-fA-F]{2})([0-9a-fA-F]{2})/.exec(x)?.splice(1, 3)
            .map(y => Math.min(Math.round(parseInt(y, 16)), 255) / 255);
        return { r: colors[0], g: colors[1], b: colors[2], a: 1 };;
    }
    public static calculateRGBfromHSL(H: number, S: number, L: number): Color01 {
        let colour = { r: 0, g: 0, b: 0, a: 1 };

        if (H == -1.0 && S == -1.0) {
            colour.r = L * 255.0;
            colour.g = L * 255.0;
            colour.b = L * 255.0;
        }
        else {
            let temporary_1;

            if (L < 0.5)
                temporary_1 = L * (1.0 + S);
            else
                temporary_1 = L + S - L * S;

            let temporary_2;

            temporary_2 = 2.0 * L - temporary_1;

            let hue = H / 360.0;

            let temporary_R = hue + 0.333;
            let temporary_G = hue;
            let temporary_B = hue - 0.333;

            if (temporary_R < 0.0)
                temporary_R += 1.0;
            if (temporary_R > 1.0)
                temporary_R -= 1.0;

            if (temporary_G < 0.0)
                temporary_G += 1.0;
            if (temporary_G > 1.0)
                temporary_G -= 1.0;

            if (temporary_B < 0.0)
                temporary_B += 1.0;
            if (temporary_B > 1.0)
                temporary_B -= 1.0;

            // RED
            if ((6.0 * temporary_R) < 1.0) {
                colour.r = (temporary_2 + (temporary_1 - temporary_2) * 6.0 * temporary_R) * 255.0;
            }
            else if ((2.0 * temporary_R) < 1.0) {
                colour.r = temporary_1 * 255.0;
            }
            else if ((3.0 * temporary_R) < 2.0) {
                colour.r = (temporary_2 + (temporary_1 - temporary_2) * (0.666 - temporary_R) * 6.0) * 255.0;
            }
            else {
                colour.r = temporary_2 * 255.0;
            }

            // GREEN
            if ((6.0 * temporary_G) < 1.0) {
                colour.g = (temporary_2 + (temporary_1 - temporary_2) * 6.0 * temporary_G) * 255.0;
            }
            else if ((2.0 * temporary_G) < 1.0) {
                colour.g = temporary_1 * 255.0;
            }
            else if ((3.0 * temporary_G) < 2.0) {
                colour.g = (temporary_2 + (temporary_1 - temporary_2) * (0.666 - temporary_G) * 6.0) * 255.0;
            }
            else {
                colour.g = temporary_2 * 255.0;
            }

            // BLUE
            if ((6.0 * temporary_B) < 1.0) {
                colour.b = (temporary_2 + (temporary_1 - temporary_2) * 6.0 * temporary_B) * 255.0;
            }
            else if ((2.0 * temporary_B) < 1.0) {
                colour.b = temporary_1 * 255.0;
            }
            else if ((3.0 * temporary_B) < 2.0) {
                colour.b = (temporary_2 + (temporary_1 - temporary_2) * (0.666 - temporary_B) * 6.0) * 255.0;
            }
            else {
                colour.b = temporary_2 * 255.0;
            }
        }

        colour.r = Math.round(Math.abs(colour.r));
        colour.g = Math.round(Math.abs(colour.g));
        colour.b = Math.round(Math.abs(colour.b));
        colour.a = 1;
        return colour;
    }

    public static getFirstSaturatedColorOrFirst(colors: Color01[]) {
        return this.getAllSaturatedColors(colors)[0] || colors[0] || "#FFFFFF";
    }
    public static getAllSaturatedColors(colors: Color01[]) {
        return colors.filter(w => Math.max(...(ObjectTyped.entries(w).map(x => [x[0], x[1] * 16] as [keyof Color01, number]).map((x, i, o) => Math.max(...o.map(y => Math.abs(x[1] - y[1])))) ?? [])) > 48);
    }

    static desaturate(x: Color01, factor: number = 0.95): Color01 {
        return ObjectTyped.fromEntries(ObjectTyped.entries(x)
            .map(y => y[0] == 'a' ? y : [y[0], (Math.min(Math.round(y[1]) * factor), 1)] as [keyof Color01, number]));
    }

    public static getDistanceHSL(color1: Color01, color2: Color01, weightsHSL: [number, number, number] = [1, 1, 1]) {
        let c1hsl = this.RGBToHSL(color1)!
        let c2hsl = this.RGBToHSL(color2)!
        let hDiff = Math.abs(c1hsl[0] - c2hsl[0]);
        if (hDiff > 180) {
            hDiff = 360 - hDiff;
        }
        return [1, 2].map((x, i) => Math.abs(c1hsl[x] - c2hsl[x]) * weightsHSL[x]).reduce((prev, curr) => prev + curr, 0) + hDiff * weightsHSL[0];
    }

    public static getDistance(color1: Color01, color2: Color01) {
        return ['r', 'g', 'b'].map(x => Math.abs(color1[x] - color2[x])).reduce((prev, curr) => prev + curr, 0);
    }

    public static RGBToHSL(color: Color01): [number, number, number] {
        const { r, g, b } = color;
        const l = Math.max(r, g, b);
        const s = l - Math.min(r, g, b);
        const h = s
            ? l === r
                ? (g - b) / s
                : l === g
                    ? 2 + (b - r) / s
                    : 4 + (r - g) / s
            : 0;
        return [
            60 * h < 0 ? 60 * h + 360 : 60 * h,
            100 * (s ? (l <= 0.5 ? s / (2 * l - s) : s / (2 - (2 * l - s))) : 0),
            (100 * (2 * l - s)) / 2,
        ];
    };
    public static RGBToHSB(color: Color01): [number, number, number] {
        const { r, g, b } = color;
        const v = Math.max(r, g, b),
            n = v - Math.min(r, g, b);
        const h =
            n === 0 ? 0 : n && v === r ? (g - b) / n : v === g ? 2 + (b - r) / n : 4 + (r - g) / n;
        return [60 * (h < 0 ? h + 6 : h), v && (n / v) * 100, v * 100];
    };

    public static rgbPerceivedLuminance(color: Color01) {
        const { r, g, b } = color;
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    static getContrastColorFor(color: Color01, prioritaryOptionsColors: Color01[] = [], minumumContrast: number = 2.9999): Color01 {
        if (prioritaryOptionsColors.length) {
            const srcStruct = this.getColorStruct(color);
            const diffs = prioritaryOptionsColors.map((x, i) => this.calculateLuminance(srcStruct, this.getColorStruct(x, i))).filter(x => x.ratio > minumumContrast)
            if (diffs.length) {
                const result = diffs.sort((a, b) => (a.dark.idx ?? a.light.idx ?? Infinity) - (b.dark.idx ?? b.light.idx ?? Infinity))[0];
                return result.dark.clr == color ? result.light.clr : result.dark.clr;
            }
        }
        const targValue = this.rgbPerceivedLuminance(color) > 0.5 ? 0 : 1;
        return { r: targValue, g: targValue, b: targValue, a: 1 };
    }

    private static getColorStruct(x: Color01, index?: number): _ColorStruct {
        return { clr: x, lum: this.rgbPerceivedLuminance(x), idx: index };
    }
    private static calculateLuminance(x: _ColorStruct, y: _ColorStruct) {
        let l = (x.lum + .05) / (y.lum + .05);
        let invert = false;
        if (l < 1) {
            l = 1 / l;
            invert = true;
        }
        return {
            dark: invert ? x : y,
            light: invert ? y : x,
            ratio: l
        };
    }

    static getBestColorContrast(colors: Color01[]): [Color01, Color01] {
        let saturatedColors = this.getAllSaturatedColors(colors);
        const executeAlgorithm = (colorArray: Color01[]): [Color01, Color01] => {
            let luminationFromColors = colorArray.map((x) => {
                return this.getColorStruct(x);
            })
            let colorPairs = luminationFromColors.flatMap(x => luminationFromColors.map(y => this.calculateLuminance(x, y))).sort((a, b) => b.ratio - a.ratio);

            let best = colorPairs[0];
            if (best.ratio < 7) {
                if (best.light.lum > .3) {
                    const dessaturateTarget = ((best.light.lum + .05) / 7 - .05) / best.dark.lum;
                    best = this.calculateLuminance(best.light, this.getColorStruct(this.desaturate(best.dark.clr, dessaturateTarget)));
                } else {
                    if (best.light.lum == 0) {
                        best.light = this.getColorStruct({ r: 1 / 255, g: 1 / 255, b: 1 / 255, a: 1 });
                    }
                    const ensaturateTarget = ((best.dark.lum + .05) * 7 - .05) / best.light.lum;
                    best = this.calculateLuminance(best.dark, this.getColorStruct(this.desaturate(best.light.clr, ensaturateTarget)));
                }
            }
            return [best.dark.clr, best.light.clr]
        }
        let fallback;
        if (saturatedColors.length > 2) {
            const result = executeAlgorithm(saturatedColors);
            return result;
        }
        return executeAlgorithm(colors);



    }
    static toRGBA(color: Color01) {
        const { r, g, b, a } = color;
        return `rgba(${r * 255},${g * 255},${b * 255},${a})`
    }



    static colorHexToRGB(color: string) {

        let r = parseInt(color.substring(1, 3), 16);
        let g = parseInt(color.substring(3, 5), 16);
        let b = parseInt(color.substring(5, 7), 16);
        return [r, g, b];
    }

    static getClampedColor(color: string) {
        var colorRgb = this.colorHexToRGB(color);
        return 'rgb(' + Math.min(colorRgb[0], 232) + "," + Math.min(colorRgb[1], 232) + "," + Math.min(colorRgb[2], 232) + ")";
    }

    static toRGB6(x: string): `#${string}` | null {
        const regexColor = this.getHexRegexParts(x);
        if (regexColor) {
            const color = regexColor[1];
            if (color.length == 3) {
                return `#${color[0]}8${color[1]}8${color[2]}8`;
            } else {
                return `#${color}`;
            }
        }
        return null;
    }

    static getHexRegexParts(val: string) {
        return /^#([0-9a-f]{3}([0-9a-f]{3})?)$/i.exec(val);
    }
}