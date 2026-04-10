window.tourInterop = {
    _tour: null,

    start: function (steps) {
        if (this._tour) {
            this._tour.cancel();
        }

        this._tour = new Shepherd.Tour({
            useModalOverlay: true,
            defaultStepOptions: {
                classes: 'solodoc-tour-step',
                scrollTo: { behavior: 'smooth', block: 'center' },
                cancelIcon: { enabled: true },
                popperOptions: {
                    modifiers: [{ name: 'offset', options: { offset: [0, 12] } }]
                }
            }
        });

        for (var i = 0; i < steps.length; i++) {
            var step = steps[i];
            var isLast = i === steps.length - 1;
            var isFirst = i === 0;

            var buttons = [];
            if (!isFirst) {
                buttons.push({
                    text: 'Tilbake',
                    action: this._tour.back,
                    classes: 'shepherd-button-secondary'
                });
            }
            if (isLast) {
                buttons.push({
                    text: 'Ferdig!',
                    action: this._tour.complete,
                    classes: 'shepherd-button-primary'
                });
            } else {
                buttons.push({
                    text: 'Neste',
                    action: this._tour.next,
                    classes: 'shepherd-button-primary'
                });
            }

            this._tour.addStep({
                id: step.id || 'step-' + i,
                title: step.title,
                text: step.text,
                attachTo: step.element ? { element: step.element, on: step.position || 'bottom' } : undefined,
                buttons: buttons
            });
        }

        this._tour.start();
    },

    cancel: function () {
        if (this._tour) {
            this._tour.cancel();
            this._tour = null;
        }
    }
};
