Array.prototype.where = function (func) {
    var items = [],
        length = this.length;

    for (var i = 0; i < length; i++) {
        if (func(this[i]))
            items.push(this[i]);
    }

    return items.length === 0 ? null : items;
}


Array.prototype.any = function (func) {
    var length = this.length;

    for (var i = 0; i < length; i++) {
        if (func(this[i]))
            return true;
    }

    return false;
}


Array.prototype.first = function (func) {
    var length = this.length,
        item = null;

    for (var i = 0; i < length; i++) {
        if (func(this[i])) {
            return this[i];
        }
    }

    return item;
}


Array.prototype.last = function (func) {
    var length = this.length - 1,
        item = null;

    for (var i = length; i >= 0; i--) {
        if (func(this[i])) {
            item = this[i];
            break;
        }
    }

    return item;
}


Array.prototype.select = function (func) {
    var length = this.length,
        items = [];

    for (var i = 0; i < length; i++) {
        items.push(func(this[i]));
    }

    return items;
}


Array.prototype.max = function (func) {
    return func === undefined ? Math.max.apply(null, this) : Math.max.apply(null, this.select(func));
};


Array.prototype.min = function (func) {
    return func === undefined ? Math.min.apply(null, this) : Math.min.apply(null, this.select(func));
};


Array.prototype.forEach = function (func) {
    var length = this.length;

    for (var i = 0; i < length; i++) {
        func(this[i]);
    }
}


Array.prototype.sum = function (func) {
    var length = this.length,
        sum = 0;

    for (var i = 0; i < length; i++) {
        sum += func(this[i]);
    }

    return sum;
}


function isUndefinedNullOrEmpty(item) {
    return item === undefined || item === null || item === '';
}