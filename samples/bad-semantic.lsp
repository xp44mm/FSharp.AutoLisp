;; Balanced but wrong AutoLISP: used to demo autolisp-check diagnostics.
(defun c:bad (x x)
  (if x)
  (setq a 1 b)
  (car x 1)
  (setq pts '(1 . 2))
  (defun g (&optional y) y)
  (princ))
