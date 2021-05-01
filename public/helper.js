export function createTixyFunction(code) {
    try {
        return new Function('t', 'i', 'x', 'y', tixyBody(code));
    } catch (error) {
        return null;
    }
}

function tixyBody(code) {
    return `
    try {
        with (Math) {
            return ${code};
        }
    } catch (error) {
        return error;
    }
`}