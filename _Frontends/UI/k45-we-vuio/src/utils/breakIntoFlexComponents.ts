import { ReactNode } from "react";

export function breakIntoFlexComponents(typeName: string): ReactNode[] {
    return typeName.split(".").map((z, i) => i > 0 ? "." + z : z)
}
