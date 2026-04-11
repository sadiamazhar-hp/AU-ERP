// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    function moveMaterialSuccessModalToBody() {
        var el = document.getElementById("materialSuccessModal");
        if (el && el.parentElement !== document.body)
            document.body.appendChild(el);
    }

    function moveMaterialDeleteConfirmModalToBody() {
        var el = document.getElementById("materialDeleteConfirmModal");
        if (el && el.parentElement !== document.body)
            document.body.appendChild(el);
    }

    /**
     * Full navigation to Materials page with ?tab=list so the server renders the List tab on first paint (no Create-tab flash).
     */
    window.materialsReloadToListTab = function () {
        try {
            var u = new URL(window.location.href);
            u.searchParams.set("tab", "list");
            var q = u.searchParams.toString();
            window.location.assign(u.pathname + (q ? "?" + q : ""));
        } catch (e) {
            window.location.reload();
        }
    };

    /**
     * Shows the teal Material success modal; runs onHiddenOnce after it is fully hidden (OK or Esc).
     */
    window.materialShowSuccessModalThen = function (message, onHiddenOnce) {
        var bodyEl = document.getElementById("materialSuccessModalBody");
        if (bodyEl) bodyEl.textContent = message || "";
        var el = document.getElementById("materialSuccessModal");
        if (!el || typeof bootstrap === "undefined" || !bootstrap.Modal) {
            if (typeof onHiddenOnce === "function") onHiddenOnce();
            return;
        }
        moveMaterialSuccessModalToBody();
        var modal = bootstrap.Modal.getOrCreateInstance(el);
        function handleHidden() {
            el.removeEventListener("hidden.bs.modal", handleHidden);
            if (typeof onHiddenOnce === "function") onHiddenOnce();
        }
        el.addEventListener("hidden.bs.modal", handleHidden);
        modal.show();
    };

    window.materialShowSuccessModal = function (message) {
        window.materialShowSuccessModalThen(message, function () { });
    };

    /**
     * Maroon WARNING modal; onConfirm runs only if user clicks OK (not Cancel / Esc / backdrop).
     */
    window.materialShowDeleteConfirmModalThen = function (message, onConfirm) {
        var text = message || "Do you want to Delete this Material?";
        var bodyEl = document.getElementById("materialDeleteConfirmModalBody");
        if (bodyEl) bodyEl.textContent = text;

        var el = document.getElementById("materialDeleteConfirmModal");
        var okBtn = document.getElementById("materialDeleteConfirmModalOk");
        if (!el || !okBtn || typeof bootstrap === "undefined" || !bootstrap.Modal) {
            if (window.confirm(text) && typeof onConfirm === "function") onConfirm();
            return;
        }

        moveMaterialDeleteConfirmModalToBody();
        var modal = bootstrap.Modal.getOrCreateInstance(el);
        var confirmed = false;

        function onOkClick(ev) {
            ev.preventDefault();
            confirmed = true;
            modal.hide();
        }

        function onHidden() {
            el.removeEventListener("hidden.bs.modal", onHidden);
            okBtn.removeEventListener("click", onOkClick);
            if (confirmed && typeof onConfirm === "function") onConfirm();
        }

        okBtn.addEventListener("click", onOkClick);
        el.addEventListener("hidden.bs.modal", onHidden, { once: true });
        modal.show();
    };
})();
