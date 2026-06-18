window.NestledNooks = window.NestledNooks || {};

window.NestledNooks.scrollToElement = (elementId) => {
    const el = document.getElementById(elementId);
    if (!el) {
        return;
    }

    el.scrollIntoView({ behavior: "smooth", block: "start" });
};

window.NestledNooks.scrollToTop = () => {
    window.scrollTo({ top: 0, left: 0, behavior: "instant" });
};
